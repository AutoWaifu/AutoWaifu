
using AutoWaifu.DataModel;
using AutoWaifu.DataModel.PerformanceTracking;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Diagnostics;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Linq;
using System.Net;
using System.Runtime.CompilerServices;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;

namespace AutoWaifu
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window, INotifyPropertyChanged
    {
        string _predictorFileName = "predictor.dat";
        string _inputCacheFileName = "cache.dat";
        string _configFileName = "config.txt";
        string _root;
        bool _running = true;
        bool _closing = false;

        List<WaifuTask> _runningTasks = new List<WaifuTask>();
        List<WaifuTask> _pendingTasks = new List<WaifuTask>();

        DateTime _lastTaskCompleteTime = DateTime.Now;




        #region Properties

        void NotifyPropertyChanged([CallerMemberName]string name = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
        }


        public string NumInputItems { get { return InputItems.Count.ToString(); } }
        public string NumOutputItems { get { return OutputItems.Count.ToString(); } }


        DateTime _lastTimeUpdate = DateTime.Now;
        string _lastTimeString;
        public string TimeRemaining
        {
            get
            {
                var now = DateTime.Now;
                if ((now - _lastTimeUpdate).TotalSeconds < 0.5)
                    return _lastTimeString;

                _lastTimeUpdate = now;
                
                var remainingItems = new List<WaifuTask>(_pendingTasks);
                if (remainingItems.Count == 0 || _closing)
                    return _lastTimeString = String.Empty;

                if (TimePredictor.Main.History.Count == 0)
                    return _lastTimeString = "Calculating time remaining...";

                double expectedTimeRemaining;
                lock (remainingItems)
                {
                    if (TimePredictor.Main.History.Count == 0)
                        return _lastTimeString = String.Empty;

                    expectedTimeRemaining = remainingItems.Sum((item) => TimePredictor.Main.ExpectedTaskDuration(item).Value.TotalSeconds);
                }

                expectedTimeRemaining -= (DateTime.Now - _lastTaskCompleteTime).TotalSeconds;
                TimeSpan timeSpan = TimeSpan.FromSeconds(expectedTimeRemaining);

                Dictionary<string, int> remainingTime = new Dictionary<string, int>
                {
                    { "day", (int)timeSpan.TotalDays },
                    { "hour", timeSpan.Hours },
                    { "minute", timeSpan.Minutes },
                    { "second", Math.Max(1, timeSpan.Seconds) }
                };

                var asList = remainingTime.ToList();
                for (int i = 0; i < asList.Count; i++)
                {
                    if (asList[i].Value > 0)
                    {
                        string result = String.Empty;

                        var majorInterval = asList[i];
                        result += String.Format("{1} {0}", majorInterval.Key, majorInterval.Value);
                        if (asList[i].Value > 1)
                            result += 's';

                        if (i != asList.Count - 1)
                        {
                            var minorInterval = asList[i + 1];
                            result += String.Format(" {1} {0}", minorInterval.Key, minorInterval.Value);
                            if (asList[i].Value > 1)
                                result += 's';
                        }

                        if (result == "1 second")
                            result += "...";

                        return _lastTimeString = result;
                    }
                }

                //  Should never happen
                throw new Exception();
            }
        }

        bool _isRunning = false;
        public bool IsRunning
        {
            get { return _isRunning; }
            set
            {
                if (_isRunning == value)
                    return;

                _isRunning = value;
                NotifyPropertyChanged();
            }
        }

        public ObservableCollection<string> InputItems { get; set; }
        public ObservableCollection<string> ProcessingItems { get; set; }
        public ObservableCollection<string> OutputItems { get; set; }

        #endregion






        #region Data Loading and Processing

        int _numProcessing = 0;


        string LocalPath(string path)
        {

            int loc;
            loc = path.ToLower().IndexOf(AppSettings.Main.InputDir.ToLower());
            if (loc != -1)
            {
                path = path.Remove(loc, AppSettings.Main.InputDir.Length);
            }

            loc = path.ToLower().IndexOf(AppSettings.Main.OutputDir.ToLower());
            if (loc != -1)
            {
                path = path.Remove(loc, AppSettings.Main.OutputDir.Length);
            }

            if (path.Length > 0)
            {
                while (path[0] == '/' || path[0] == '\\')
                {
                    path = path.Substring(1);
                }
            }


            return path;
        }

        List<string> _acceptedExts = new List<string>()
            {
                ".png",
                ".jpg",
                ".jpeg",
                ".gif"
            };

        public event PropertyChangedEventHandler PropertyChanged;

        void LoadInputDirectory(String dir)
        {
            //bool success = ThreadPool.SetMaxThreads(6, 6);

            var localDir = LocalPath(dir);

            var inputPath = Path.Combine(AppSettings.Main.InputDir, localDir);
            if (!Directory.Exists(inputPath))
                Directory.CreateDirectory(inputPath);

            if (!Directory.Exists(inputPath))
            {
                MessageBox.Show(String.Format("Can't access input directory \"{0}\"", inputPath));
                return;
            }

            var inputFileInfo = new DirectoryInfo(inputPath).GetFiles();



            foreach (var file in Directory.EnumerateFiles(dir)
                              .Where(f => _acceptedExts.Contains(Path.GetExtension(f).ToLower()))
                              .Where(f => !OutputItems.Contains(LocalPath(Path.GetFullPath(f))))
                              .Where(f => !InputItems.Contains(LocalPath(Path.GetFullPath(f)))))
            {
                Task.Run(async () =>
                {
                    try
                    {
                        var task = new WaifuTask
                        {
                            TaskName = LocalPath(Path.GetFullPath(file))
                        };

                        //InputMetricCacheItem cachedItem;
                        //lock (InputMetricCache.Main.CacheItems)
                        //{
                        //    cachedItem = InputMetricCache.Main.CacheItems.SingleOrDefault(i => i.FullPath == Path.GetFullPath(file));
                        //}
                        //if (cachedItem != null)
                        //{
                        //    var updatedItem = task.LoadForInputCache(cachedItem, inputFileInfo.Single(fi => fi.FullName == Path.GetFullPath(file)));
                        //    if (updatedItem != null)
                        //    {
                        //        InputMetricCache.Main.Remove(cachedItem);
                        //        InputMetricCache.Main.Add(updatedItem);
                        //    }
                        //}
                        //else
                        //{
                        //    cachedItem = task.LoadForInput(Path.GetFullPath(file));
                        //    InputMetricCache.Main.Add(cachedItem);
                        //}

                        await Task.Delay(50);

                        lock (_pendingTasks)
                            _pendingTasks.Add(task);

                        await Dispatcher.BeginInvoke(new Action(() =>
                        {
                            InputItems.Add(LocalPath(Path.GetFullPath(file)));
                        }));
                    }
                    catch (Exception e)
                    {
#if DEBUG
                        Debugger.Break();
#endif
                        throw;
                    }
                });
            }

            foreach (var subdir in Directory.EnumerateDirectories(dir))
                LoadInputDirectory(subdir);
        }

        void LoadOutputDirectory(string dir)
        {
            if (!Directory.Exists(dir))
                Directory.CreateDirectory(dir);

            if (!Directory.Exists(dir))
            {
                MessageBox.Show(String.Format("Can't access output directory \"{0}\"", dir));
            }

            foreach (var file in Directory.EnumerateFiles(dir)
                                .Where(f => _acceptedExts.Contains(Path.GetExtension(f).ToLower())))
            {
                OutputItems.Add(LocalPath(Path.GetFullPath(file)));
            }

            foreach (var subdir in Directory.EnumerateDirectories(dir))
                LoadOutputDirectory(subdir);
        }

        #endregion
        





        #region Initialization
        public MainWindow()
        {
            InputItems = new ObservableCollection<string>();
            ProcessingItems = new ObservableCollection<string>();
            OutputItems = new ObservableCollection<string>();

            InputItems.CollectionChanged += (a, b) =>
            {
                NotifyPropertyChanged(nameof(NumInputItems));
                NotifyPropertyChanged(nameof(TimeRemaining));
            };

            ProcessingItems.CollectionChanged += (a, b) =>
            {
                NotifyPropertyChanged(nameof(TimeRemaining));
            };

            OutputItems.CollectionChanged += (a, b) =>
            {
                NotifyPropertyChanged(nameof(NumOutputItems));
            };




            this.DataContext = this;
            InitializeComponent();

            _root = ".";

            var desktopDir = @"D:\Storage\ImageAutoScale";
            if (Directory.Exists(desktopDir))
                _root = desktopDir;

            _root = Path.GetFullPath(_root);

            _configFileName = Path.Combine(_root, _configFileName);
            _inputCacheFileName = Path.Combine(_root, _inputCacheFileName);
            _predictorFileName = Path.Combine(_root, _predictorFileName);

            if (File.Exists(_configFileName))
            {
                AppSettings.Main.LoadFrom(File.ReadAllText(_configFileName));
            }
            else
            {
                File.WriteAllText(_configFileName, AppSettings.Main.SaveTo(String.Empty));
            }

            TimePredictor.Main.LoadFrom(_predictorFileName);
            InputMetricCache.Main.LoadFrom(_inputCacheFileName);


            AppSettings.Main.Waifu2xCaffeDir = Path.Combine(_root, AppSettings.Main.Waifu2xCaffeDir);
            AppSettings.Main.OutputDir = Path.Combine(_root, AppSettings.Main.OutputDir);
            AppSettings.Main.InputDir = Path.Combine(_root, AppSettings.Main.InputDir);
            AppSettings.Main.TempDir = Path.Combine(_root, AppSettings.Main.TempDir);


            try
            {
                //if (Directory.Exists(AppSettings.Main.TempDir))
                //    Directory.Delete(AppSettings.Main.TempDir, true);
            }
            catch {}

            Directory.CreateDirectory(AppSettings.Main.TempDir);
            Directory.CreateDirectory(AppSettings.Main.TempDirInput);
            Directory.CreateDirectory(AppSettings.Main.TempDirOutput);

            //AppSettings.Main.MaxParallel = Math.Max(1, (int)Math.Floor(AppSettings.Main.MaxParallel / AppSettings.Main.SuperSamples));

            try
            {
                LoadOutputDirectory(AppSettings.Main.OutputDir);
                LoadInputDirectory(AppSettings.Main.InputDir);

                FileSystemWatcher inputWatcher = new FileSystemWatcher();
                inputWatcher.Path = AppSettings.Main.InputDir;
                inputWatcher.IncludeSubdirectories = true;
                inputWatcher.Created += InputWatcher_Created;
                inputWatcher.Deleted += InputWatcher_Deleted;
                inputWatcher.EnableRaisingEvents = true;

                FileSystemWatcher outputWatcher = new FileSystemWatcher();
                outputWatcher.Path = AppSettings.Main.OutputDir;
                outputWatcher.IncludeSubdirectories = true;
                outputWatcher.Deleted += OutputWatcher_Deleted;
                outputWatcher.EnableRaisingEvents = true;
            }
            catch (Exception e)
            {

            }

            Task.Run(async () =>
            {
                while (true)
                {
                    await Task.Delay(1000);
                    await Dispatcher.BeginInvoke(new Action(() =>
                    {
                        NotifyPropertyChanged(nameof(TimeRemaining));
                    }));
                }
            });

            Task.Run(async () =>
            {
                while (_running)
                {
                    while (InputItems.Count > 0 && ProcessingItems.Count < AppSettings.Main.MaxParallel && _running)
                    {
                        Dispatcher.Invoke(new Action(() =>
                        {
                            var item = InputItems[0];
                            InputItems.Remove(item);
                            ProcessingItems.Add(item);
                            Task.Run(async () =>
                            {
                                WaifuTask task;
                                lock (_pendingTasks)
                                    task = _pendingTasks.Single(t => t.TaskName == item);
                                var inputPath = Path.Combine(AppSettings.Main.InputDir, item);
                                var inputFile = Path.GetFullPath(inputPath);
                                Interlocked.Increment(ref _numProcessing);

                                lock (_runningTasks)
                                    _runningTasks.Add(task);

                                try
                                {
                                    await task.Start(inputFile, item);
                                    Interlocked.Decrement(ref _numProcessing);
                                }
                                catch (Exception e)
                                {
                                    Interlocked.Decrement(ref _numProcessing);
                                    throw;
                                }

                                lock (_runningTasks)
                                    _runningTasks.Remove(task);
                                lock (_pendingTasks)
                                    _pendingTasks.Remove(task);

                                if (!task.WasTerminated)
                                    TimePredictor.Main.History.Add(task.PerformanceData);

                                await Dispatcher.BeginInvoke(new Action(() =>
                                {
                                    _lastTaskCompleteTime = DateTime.Now;
                                    ProcessingItems.Remove(item);
                                    OutputItems.Add(item);
                                }));
                            });
                        }));
                    }

                    await Task.Delay(1);
                }
            });
        }

        #endregion






        #region File Watchers

        private void OutputWatcher_Deleted(object sender, FileSystemEventArgs e)
        {
            Dispatcher.BeginInvoke(new Action(() =>
            {
                InputItems.Add(LocalPath(e.FullPath));
                var task = new WaifuTask
                {
                    TaskName = LocalPath(e.FullPath)
                };

                task.LoadForInput(Path.GetFullPath(Path.Combine(AppSettings.Main.InputDir, LocalPath(e.FullPath))));

                lock (_pendingTasks)
                    _pendingTasks.Add(task);

                var toRemove = OutputItems.Single(f => Path.GetFileNameWithoutExtension(f) == Path.GetFileNameWithoutExtension(e.FullPath));
                int index = OutputItems.IndexOf(toRemove);
                OutputItems.RemoveAt(index);
            }));
        }

        private void InputWatcher_Deleted(object sender, FileSystemEventArgs e)
        {
            string name = LocalPath(Path.GetFileName(e.FullPath));
            if (Path.GetExtension(name) == "")
                return;

            Dispatcher.BeginInvoke(new Action(() =>
            {
                if (InputItems.Contains(name))
                {
                    InputItems.Remove(name);

                    lock (_pendingTasks)
                        _pendingTasks.Remove(_pendingTasks.Single(t => t.TaskName == name));
                }
            }));
        }

        private void InputWatcher_Created(object sender, FileSystemEventArgs e)
        {
            string name = Path.GetFileName(e.FullPath);
            if (Path.GetExtension(name) == "")
                return;

            Dispatcher.BeginInvoke(new Action(() =>
            {
                LoadInputDirectory(Path.GetDirectoryName(e.FullPath));
            }));
        }

        #endregion






        #region UI Events


        protected override void OnClosing(CancelEventArgs e)
        {
            _running = false;
            _closing = true;

            if (_numProcessing > 0)
            {
                if (MessageBox.Show(this, "There are pending conversions, do you want to terminate them?", "", MessageBoxButton.YesNoCancel) == MessageBoxResult.Yes)
                {
                    while (_runningTasks.Count > 0)
                    {
                        var task = _runningTasks[0];
                        task.Teriminate();
                    }

                    while (_numProcessing > 0)
                    {
                        Task.Delay(20).Wait();
                    }
                }
                else if (ClosingLabel.Visibility == Visibility.Hidden)
                {
                    string closingLabelText = ClosingLabel.Content as String;

                    Task.Run(new Action(async () =>
                    {
                        while (_numProcessing != 0)
                        {
                            await Dispatcher.BeginInvoke(new Action(() =>
                            {
                                ClosingLabel.Content = closingLabelText + " (" + _numProcessing + ")";
                            }));

                            await Task.Delay(200);
                        }

                        await Dispatcher.BeginInvoke(new Action(() =>
                        {
                            this.Close();
                        }));
                    }));

                    e.Cancel = true;
                    ClosingLabel.Visibility = Visibility.Visible;
                }
            }

            TimePredictor.Main.SaveTo(_predictorFileName);
            InputMetricCache.Main.SaveTo(_inputCacheFileName);

            base.OnClosing(e);
        }


        private void SettingsButton_Click(object sender, RoutedEventArgs e)
        {
            var settingsWindow = new SettingsWindow();
            settingsWindow.Owner = this;
            settingsWindow.ViewModel = AppSettings.Main.Copy();

            if (settingsWindow.ShowDialog() == true)
            {
                AppSettings.SetMainSettings(settingsWindow.Result);
                var configFile = AppSettings.Main.SaveTo(File.ReadAllText(_configFileName));
                File.WriteAllText(_configFileName, configFile);
            }
        }

        bool _isRestarting = false;
        private void RestartAllButton_Click(object sender, RoutedEventArgs e)
        {
            if (_isRestarting)
                return;
            
            void DeleteFilesInDirectory(string path)
            {
                foreach (var file in Directory.EnumerateFiles(path))
                {
                    Task.Run(async () =>
                    {
                        bool isReady = false;
                        while (!isReady)
                        {
                            try
                            {
                                if (File.Exists(file))
                                    File.Delete(file);
                                isReady = true;
                            }
                            catch (Exception ex)
                            {
                                isReady = false;
                                await Task.Delay(100);
                            }
                        }
                    });
                }

                foreach (var directory in Directory.EnumerateDirectories(path))
                    DeleteFilesInDirectory(directory);
            }

            DeleteFilesInDirectory(AppSettings.Main.OutputDir);
        }

        
        private void InputListBox_Drop(object sender, DragEventArgs e)
        {
            Task.Run(async () =>
            {
                if (e.Data.GetDataPresent(DataFormats.Html))
                {
                    string html = e.Data.GetData(DataFormats.Html) as string;

                    html = html.Replace('\n', ' ');
                    html = html.Replace('\r', ' ');

                    var matchImage = new Regex(@"<img (.*)\/?>");
                    var matchSrc = new Regex("src=[\"']([\\w:\\/\\\\\\.\\d-=\\?]*)");

                    var imageMatch = matchImage.Match(html);
                    var img = imageMatch.Groups[1].Value;

                    var srcMatch = matchSrc.Match(img);
                    var src = srcMatch.Groups[1].Value;

                    var type = Path.GetExtension(src);
                    switch (type)
                    {
                        case ".png":
                            break;

                        case ".jpeg":
                            break;

                        case ".jpg":
                            break;

                        case ".gif":
                            break;

                        default:
                            return;
                    }


                    String tempFileName = Guid.NewGuid().ToString();

                    WebClient client = new WebClient();
                    await client.DownloadFileTaskAsync(src, tempFileName);

                    File.Copy(tempFileName, Path.GetFullPath(Path.Combine(AppSettings.Main.InputDir, Path.GetFileName(src))));
                    File.Delete(tempFileName);
                }
            });

            if (e.Data.GetDataPresent(DataFormats.Bitmap))
            {

            }

            if (e.Data.GetDataPresent(DataFormats.FileDrop))
            {

            }
        }

        #endregion
    }
}
