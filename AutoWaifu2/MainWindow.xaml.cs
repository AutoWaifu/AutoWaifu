using AutoWaifu.Lib.Waifu2x;
using Serilog;
using Serilog.Events;
using Serilog.Formatting.Display;
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
        ILogger Logger { get; set; }
        ProcessingStatus currentStatus;
        StatusServer statusServer;

        

        public MainWindow()
        {
            InitializeComponent();

            CheckExistingProcesses("waifu2x-caffe-cui");
            CheckExistingProcesses("ffmpeg");

            var cmdLine = Environment.GetCommandLineArgs().Select(s => s.ToLower());
            if (cmdLine.Any(c => c == "-headless"))
            {
                RootConfig.IsHeadless = true;
                this.Hide();
            }
            
            if (cmdLine.Any(c => c == "-status-server"))
            {
                RootConfig.UseStatusServer = true;
            }

            RootConfig.AppDispatcher = this.Dispatcher;

            App.Logged += (level, msg) =>
            {
                if (level >= LogEventLevel.Warning)
                    ErrorLogPage.LogMessage(level, msg);

                FullLogPage.LogMessage(level, msg);

#if DEBUG
                if (RootConfig.IsHeadless)
                    Debug.WriteLine(msg);
#endif
            };

            Logger = Log.ForContext<MainWindow>();

            MediaViewer_MediaElementPlayer.MediaOpened += MediaViewer_MediaElementPlayer_MediaOpened;
            MediaViewer_MediaElementPlayer.MediaEnded += MediaViewer_MediaElementPlayer_MediaEnded;

            this.Closing += MainWindow_Closing;

            ViewModel = new MainWindowViewModel();

            //  Initialize manually since window resize won't be fired
            if (RootConfig.IsHeadless)
                InitializeViewModel();
        }

        bool didInitialize = false;
        void InitializeViewModel()
        {
            if (this.didInitialize)
                return;

            ViewModel.Initialize(this.Dispatcher);

            this.currentStatus = new ProcessingStatus();

            var viewModel = ViewModel;

            void UpdateStatusFromViewModel()
            {
                this.currentStatus.NumComplete = viewModel.CompletedOutputFiles.Count;
                this.currentStatus.NumPending = viewModel.PendingInputFiles.Count;
                this.currentStatus.NumProcessing = viewModel.ProcessingQueueFiles.Count;
                this.currentStatus.NumImagesProcessing = -1;
                this.currentStatus.NumImagesProcessingPending = -1;

                this.currentStatus.ProcessingQueueStates = string.Join("\n", viewModel.ProcessingQueueFiles.Select(ti => ti.TaskState));
            }

            UpdateStatusFromViewModel();

            ViewModel.TaskItems.CollectionChanged += (s, e) =>
            {
                UpdateStatusFromViewModel();
            };

            ViewModel.TaskItems.TaskItemChanged += (t) =>
            {
                UpdateStatusFromViewModel();
            };

            if (ViewModel.Settings.AutoStartOnOpen || RootConfig.IsHeadless)
            {
                Logger.Debug("Auto-starting processing queue since AutoStartOnOpen=true, or running in headless mode");
                ViewModel.StartProcessing();
            }

            ViewModel.PropertyChanged += ViewModel_PropertyChanged;


            PendingFilesPane.Title = ViewModel.PendingFileListLabel;
            ProcessingFilesPane.Title = ViewModel.ProcessingFileListLabel;
            OutputFilesPane.Title = ViewModel.OutputFileListLabel;

            this.statusServer = new StatusServer(this.currentStatus);
            try
            {
                this.statusServer.Start();
            }
            catch (Exception e)
            {
                Logger.Error("Unable to start status server: {Exception}", e.ToString());
            }

            this.didInitialize = true;
        }

        void CheckExistingProcesses(string processName)
        {
            var processes = Process.GetProcesses()
                                   .Where(p => p.ProcessName.Contains(processName))
                                   .ToArray();

            if (processes.Length > 0)
            {
                string message = $"There are {processes.Length} running instances of {processName}, would you like to terminate them?";

                if (MessageBox.Show(message, "Confirm", MessageBoxButton.YesNo) == MessageBoxResult.Yes)
                {
                    foreach (var inst in processes)
                        inst.Kill();
                }
            }
        }

        protected override void OnRenderSizeChanged(SizeChangedInfo sizeInfo)
        {
            base.OnRenderSizeChanged(sizeInfo);
            InitializeViewModel();
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

                case nameof(MainWindowViewModel.IsProcessing):
                    if (ViewModel.IsProcessing)
                        this.currentStatus.QueueStatus = "Running";
                    else
                        this.currentStatus.QueueStatus = "Stopped";
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
                App.IsClosing = true;
                Log.CloseAndFlush();

                ViewModel.CleanTempFolders();

                FormatLog("log.txt");
                FormatLog("log.json");

                this.statusServer?.Stop();
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

            MediaViewer_MediaList.SelectedItem = null;

            var items = OutputFilesList.SelectedItems.Cast<TaskItem>();

            Task.Run(async () =>
            {
                foreach (var item in items)
                {
                    bool wasSuccessful = true;

                    do
                    {
                        try
                        {
                            File.Delete(item.OutputPath);
                        }
                        catch
                        {
                            wasSuccessful = false;
                        }

                        if (!wasSuccessful)
                            await Task.Delay(10);
                    }
                    while (!wasSuccessful);
                }
            });
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


            Log.Verbose("Selecting {ImageFile} in Media View", selectedItem.InputPath);


            if (isImage || !File.Exists("EVRPresenter64.dll") || !File.Exists("DirectShowLib-2005.dll"))
            {
                Log.Verbose("Loading using WPF Core Media Element");

                MediaViewer_MediaElementPlayer.Source = new Uri(mediaPath, UriKind.Absolute);
                MediaViewer_MediaElementPlayer.Play();

                MediaViewer_MediaElementPlayer.Visibility = Visibility.Visible;
                MediaViewer_MediaKitPlayer.Visibility = Visibility.Hidden;
            }
            else
            {
                Log.Verbose("Loading using WPF Toolkit");

                MediaViewer_MediaKitPlayer.Source = new Uri(mediaPath, UriKind.Absolute);
                MediaViewer_MediaKitPlayer.Loop = true;

                MediaViewer_MediaElementPlayer.Visibility = Visibility.Hidden;
                MediaViewer_MediaKitPlayer.Visibility = Visibility.Visible;
            }
        }
        


        string FormatLog(string logPath)
        {
            File.Copy("log.txt", "log2.txt");
            var log = File.ReadAllText("log2.txt");
            log = log.Replace(@"\\", @"\");
            log = log.Replace("\\\"", "\"");

            File.Delete("log2.txt");
            

            return log;
        }

        private void StartProcessingButton_Click(object sender, RoutedEventArgs e)
        {
            ViewModel.StartProcessing();
        }

        private async void StopProcessingButton_Click(object sender, RoutedEventArgs e)
        {
            await ViewModel.StopProcessing();
        }

        private void CopyFullLogButton_Click(object sender, RoutedEventArgs e)
        {
            string log = FormatLog("log.txt");

            log = FileSystemHelper.AnonymizeFilePaths(log);

            Clipboard.SetText(log);
        }
    }
}
