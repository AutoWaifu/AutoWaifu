using AutoWaifu.Lib.Waifu2x;
using Serilog;
using Serilog.Events;
using Serilog.Formatting.Json;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Threading;

namespace AutoWaifu2
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        ILogger Logger = Log.ForContext<MainWindow>();

        class LogObserver : IObserver<LogEvent>
        {
            public void OnCompleted()
            {
                
            }

            public void OnError(Exception error)
            {
                
            }

            public void OnNext(LogEvent value)
            {
                var stringBuilder = new StringBuilder();
                using (TextWriter writer = new StringWriter(stringBuilder))
                {
                    value.RenderMessage(writer);
                    
                    NewMessageEvent?.Invoke(value.Level, value.Level.ToString() + ": " + stringBuilder.ToString());
                }
            }

            event Action<LogEventLevel, string> NewMessageEvent;

            public LogObserver OnMessage(Action<LogEventLevel, string> msgCallback)
            {
                NewMessageEvent += msgCallback;
                return this;
            }
        }

        public MainWindow()
        {
            InitializeComponent();

            var cmdLine = Environment.GetCommandLineArgs();
            if (cmdLine.Any(c => c.ToLower() == "-headless"))
                this.Hide();

            RootConfig.AppDispatcher = this.Dispatcher;

            if (File.Exists("log.txt"))
                File.Delete("log.txt");
            if (File.Exists("log.json"))
                File.Delete("log.json");

            Serilog.Debugging.SelfLog.Enable((msg) =>
            {
                Debug.WriteLine("Serilog: " + msg);
            });

            AppDomain.CurrentDomain.DomainUnload += (o, e) =>
            {
                FormatLog("log.txt");
                FormatLog("log.json");
            };


#if DEBUG
            StackifyLib.Logger.ApiKey = null;
            StackifyLib.Utils.StackifyAPILogger.LogEnabled = true;
            StackifyLib.Utils.StackifyAPILogger.OnLogMessage += (string data) =>
            {
                Debug.WriteLine(data);
            };
#endif


            Log.Logger = new LoggerConfiguration()
                            .MinimumLevel.Verbose()
                            .WriteTo.File("log.txt")
                            .WriteTo.File(new JsonFormatter(null, true), "log.json")
//#if DEBUG
//                            .WriteTo.Stackify()
//#endif
                            .WriteTo.Observers(events => events.Subscribe(new LogObserver().OnMessage((level, msg) =>
                            {
#if DEBUG
                                //if (level >= LogEventLevel.Error && Debugger.IsAttached)
                                //    Debugger.Break();
#endif

                                if (level >= LogEventLevel.Warning)
                                    InfoLogPage.LogMessage(level, msg);

                                DebugLogPage.LogMessage(level, msg);
                            })))
                            .CreateLogger();



            MediaViewer_MediaElementPlayer.MediaOpened += MediaViewer_MediaElementPlayer_MediaOpened;
            MediaViewer_MediaElementPlayer.MediaEnded += MediaViewer_MediaElementPlayer_MediaEnded;

            this.Closing += MainWindow_Closing;

            ViewModel = new MainWindowViewModel();
            ViewModel.Initialize(this.Dispatcher);

            ViewModel.StartProcessing();

            ViewModel.PropertyChanged += ViewModel_PropertyChanged;


            PendingFilesPane.Title = ViewModel.PendingFileListLabel;
            ProcessingFilesPane.Title = ViewModel.ProcessingFileListLabel;
            OutputFilesPane.Title = ViewModel.OutputFileListLabel;
        }



        private void ViewModel_PropertyChanged(object sender, System.ComponentModel.PropertyChangedEventArgs e)
        {
            switch (e.PropertyName)
            {
                case nameof(MainWindowViewModel.PendingFileListLabel):
                    PendingFilesPane.Title = ViewModel.PendingFileListLabel;
                    break;

                case nameof(MainWindowViewModel.ProcessingFileListLabel):
                    ProcessingFilesPane.Title = ViewModel.ProcessingFileListLabel;
                    break;

                case nameof(MainWindowViewModel.OutputFileListLabel):
                    OutputFilesPane.Title = ViewModel.OutputFileListLabel;
                    break;
            }
        }

        private void MediaViewer_MediaElementPlayer_MediaOpened(object sender, RoutedEventArgs e)
        {
            MediaViewer_MediaElementPlayer.Play();
        }


        private void MediaViewer_MediaElementPlayer_MediaEnded(object sender, RoutedEventArgs e)
        {
            MediaViewer_MediaElementPlayer.Position = TimeSpan.FromMilliseconds(1);
            MediaViewer_MediaElementPlayer.Play();
        }

        bool isClosing = false;
        bool canClose = false;
        private void MainWindow_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            if (canClose)
                return;

            if (isClosing)
            {
                e.Cancel = true;
                return;
            }

            //  Reference on the stack since STA apps don't allow access from other threads
            var viewModel = ViewModel;

            if (viewModel.ProcessingQueueFiles.Count > 0)
            {
                var shouldForceQuit = MessageBox.Show("There are pending tasks in the queue, would you like to terminate them?", string.Empty, MessageBoxButton.YesNoCancel);

                switch (shouldForceQuit)
                {
                    case MessageBoxResult.Yes:
                        WaitingForTasksLbl.Visibility = Visibility.Visible;

                        e.Cancel = true;
                        isClosing = true;

                        Task.Run(() =>
                        {
                            Logger.Verbose("Force-quitting existing processes");

                            DateTime waitStartTime = DateTime.Now;
                            var stopProcessingTask = viewModel.StopProcessing();

                            while (!stopProcessingTask.Wait(10))
                            {
                                DateTime now = DateTime.Now;

                                if ((now - waitStartTime).TotalSeconds > 5000)
                                {
                                    ProcessHelper.Terminate("waifu2x-caffe-cui.exe");
                                    ProcessHelper.Terminate("ffmpeg.exe");

                                    break;
                                }
                            }

                            canClose = true;

                            Dispatcher.Invoke(() => this.Close());
                        });
                        break;

                    case MessageBoxResult.No:
                        WaitingForTasksLbl.Visibility = Visibility.Visible;

                        e.Cancel = true;
                        isClosing = true;
                        WaitingForTasksLbl.Visibility = Visibility.Visible;

                        DispatcherTimer refreshTimer = new DispatcherTimer();
                        refreshTimer.Interval = TimeSpan.FromSeconds(1);
                        refreshTimer.Tick += (a, b) => WaitingForTasksLbl.Content = $"Waiting for {viewModel.ProcessingQueueFiles.Count} tasks...";
                        refreshTimer.Start();

                        Task.Run(async () =>
                        {
                            await viewModel.WaitForProcessingToFinish();
                            canClose = true;

                            Dispatcher.Invoke(() => this.Close());
                        });
                        break;

                    default:
                        e.Cancel = true;
                        break;
                }
            }




            if (!e.Cancel)
            {
                Log.CloseAndFlush();

                ViewModel.CleanTempFolders();

                FormatLog("log.txt");
                FormatLog("log.json");
            }
        }

        public MainWindowViewModel ViewModel
        {
            get { return this.DataContext as MainWindowViewModel; }
            set { this.DataContext = value; }
        }

        private void SettingsButton_Click(object sender, RoutedEventArgs e)
        {
            var settingsWindow = new SettingsWindow();
            settingsWindow.Owner = this;
            var settingsCopy = ViewModel.Settings.Copy();
            settingsWindow.ViewModel = new AppSettingsViewModel(settingsCopy);

            if (settingsWindow.ShowDialog().Value)
            {
                AppSettings.SetMainSettings(settingsCopy);
                ViewModel.Settings = settingsCopy;
                ViewModel.Settings.SaveToFile(RootConfig.SettingsFilePath);
            }
        }

        private void OutputCtxMenuRestartItem_Click(object sender, RoutedEventArgs e)
        {
            if (MessageBoxResult.Yes != MessageBox.Show("Are you sure you want to restart these tasks? The current output files will be deleted.", "", MessageBoxButton.YesNo))
                return;

            var items = OutputFilesList.SelectedItems.Cast<TaskItem>();
            foreach (var item in items)
                File.Delete(item.OutputPath);
        }

        private void OutputCtxMenuOpenOutputFolder_Click(object sender, RoutedEventArgs e)
        {
            var task = OutputFilesList.SelectedItem as TaskItem;
            if (task == null)
                return;

            ProcessStartInfo psi = new ProcessStartInfo();
            psi.FileName = "explorer";
            psi.Arguments = $"/select, \"{task.OutputPath}\"";

            Process.Start(psi);
        }

        private void Window_PreviewDrop(object sender, DragEventArgs e)
        {

        }

        private void Window_Drop(object sender, DragEventArgs e)
        {
            Task.Run(async () =>
            {
                if (e.Data.GetDataPresent(DataFormats.Html))
                {
                    string html = e.Data.GetData(DataFormats.Html) as string;

                    html = html.Replace('\n', ' ');
                    html = html.Replace('\r', ' ');
                    html = html.Replace("&amp;", "&");

                    var matchImage = new Regex(@"<img (.*)\/?>");
                    var matchSrc = new Regex("src=[\"']([\\w:\\/\\\\\\.\\d-=\\?\\&\\%;]*)\"");

                    var imageMatch = matchImage.Match(html);
                    var img = imageMatch.Groups[1].Value;

                    var srcMatch = matchSrc.Match(img);
                    var src = srcMatch.Groups[1].Value;

                    var matchFileName = new Regex(@"([\w\d]+\.(gif|png|jpg|jpeg))");
                    var fileNameMatch = matchFileName.Match(src);
                    var fileName = fileNameMatch.Captures[0].Value;

                    var type = Path.GetExtension(fileName);
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


                    string tempFileName = fileName + '-' + Guid.NewGuid().ToString();
                    string tempFilePath = Path.Combine(AppSettings.Main.TempDir, tempFileName);

                    WebClient client = new WebClient();
                    await client.DownloadFileTaskAsync(src, tempFilePath);

                    string newFilePath = Path.GetFullPath(Path.Combine(AppSettings.Main.InputDir, fileName));
                    if (File.Exists(newFilePath))
                        File.Delete(newFilePath);

                    File.Move(tempFilePath, newFilePath);
                }
            });

            if (e.Data.GetDataPresent(DataFormats.FileDrop))
            {
                var files = e.Data.GetData(DataFormats.FileDrop) as string[];

                switch (ViewModel.Settings.FileDragMethod)
                {
                    case AppSettings.ImportFileMethod.Copy:
                        Logger.Verbose("Copying files that were dragged onto AutoWaifu");
                        foreach (var file in files)
                        {
                            var outputPath = AppRelativePath.CreateInput(Path.GetFileName(file));
                            Logger.Verbose("Copying {@InputFile} to {@OutputFile}", file, outputPath);
                            FileSystemHelper.RecursiveCopy(file, outputPath);
                        }
                        break;

                    case AppSettings.ImportFileMethod.Move:
                        Logger.Verbose("Moving files that were dragged onto AutoWaifu");
                        foreach (var file in files)
                        {
                            var outputPath = AppRelativePath.CreateInput(Path.GetFileName(file));
                            Logger.Verbose("Moving {@InputFile} to {@OutputFile}", file, outputPath);
                            FileSystemHelper.RecursiveMove(file, outputPath);
                        }
                        break;
                }
            }
        }

        private void MediaViewListBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (MediaViewer_MediaList.SelectedItem == null)
            {
                MediaViewer_MediaKitPlayer.Source = null;
                MediaViewer_MediaElementPlayer.Source = null;
                return;
            }

            var selectedItem = MediaViewer_MediaList.SelectedItem as TaskItem;
            string mediaPath = Path.Combine(AppSettings.Main.InputDir, selectedItem.InputPath);
            if (selectedItem.State == TaskItemState.Done)
                mediaPath = Path.Combine(AppSettings.Main.OutputDir, selectedItem.OutputPath);

            string mediaExt = Path.GetExtension(mediaPath);
            bool isImage = mediaExt == ".png" ||
                                mediaExt == ".jpg" ||
                                mediaExt == ".jpeg";



            if (isImage)
            {
                MediaViewer_MediaElementPlayer.Source = new Uri(mediaPath, UriKind.Absolute);
                MediaViewer_MediaElementPlayer.Play();

                MediaViewer_MediaElementPlayer.Visibility = Visibility.Visible;
                MediaViewer_MediaKitPlayer.Visibility = Visibility.Hidden;
            }
            else
            {
                MediaViewer_MediaKitPlayer.Source = new Uri(mediaPath, UriKind.Absolute);
                MediaViewer_MediaKitPlayer.Loop = true;

                MediaViewer_MediaElementPlayer.Visibility = Visibility.Hidden;
                MediaViewer_MediaKitPlayer.Visibility = Visibility.Visible;
            }
        }
        


        void FormatLog(string logPath)
        {
            var log = File.ReadAllText(logPath);
            log = log.Replace(@"\\", @"\");
            log = log.Replace("\\\"", "\"");

            //log = FileSystemHelper.AnonymizeFilePaths(log);

            File.WriteAllText(logPath, log);
        }

        private void StartProcessingButton_Click(object sender, RoutedEventArgs e)
        {
            ViewModel.StartProcessing();
        }

        private async void StopProcessingButton_Click(object sender, RoutedEventArgs e)
        {
            await ViewModel.StopProcessing();
        }
    }
}
