using AutoWaifu.Lib.Waifu2x;
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
using WaifuLog;

namespace AutoWaifu2
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            InitializeComponent();

            RootConfig.AppDispatcher = this.Dispatcher;

            string logFile = "log.txt";

            File.AppendAllText(logFile, $"\n\n\n========== Log started for {DateTime.Now}\n");

            WaifuLogger.LogWritten += (t, m) =>
            {
                File.AppendAllText("log.txt", m + '\n');
            };

            var infoLogStream = new WaifuLoggerStream
            {
                MessageFilter = WaifuLogger.LogMessageType.ConfigWarning | WaifuLogger.LogMessageType.ExternalError | WaifuLogger.LogMessageType.LogicError | WaifuLogger.LogMessageType.Warning
            };

            var debugLogStream = new WaifuLoggerStream
            {
                MessageFilter = WaifuLogger.LogMessageType.All
            };

            InfoLogPage.LogStream = infoLogStream;
            DebugLogPage.LogStream = debugLogStream;

            this.Closing += MainWindow_Closing;

            ViewModel = new MainWindowViewModel();
            ViewModel.Initialize(this.Dispatcher);
            ViewModel.StartProcessing();

            try
            {
                throw new Exception();
            }
            catch (Exception e)
            {
                WaifuLogger.Exception(e);
            }
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
                        e.Cancel = true;
                        isClosing = true;

                        Task.Run(async () =>
                        {
                            await viewModel.StopProcessing();
                            canClose = true;

                            Dispatcher.Invoke(() => this.Close());
                        });
                        break;

                    case MessageBoxResult.No:
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


                    string tempFileName = Path.GetFileName(src) + '-' + Guid.NewGuid().ToString();
                    string tempFilePath = Path.Combine(AppSettings.Main.TempDir, tempFileName);

                    WebClient client = new WebClient();
                    await client.DownloadFileTaskAsync(src, tempFilePath);

                    string newFilePath = Path.GetFullPath(Path.Combine(AppSettings.Main.InputDir, Path.GetFileName(src)));
                    if (File.Exists(newFilePath))
                        File.Delete(newFilePath);

                    File.Move(tempFilePath, newFilePath);
                }
            });

            if (e.Data.GetDataPresent(DataFormats.Bitmap))
            {

            }

            if (e.Data.GetDataPresent(DataFormats.FileDrop))
            {

            }
        }

        private void MediaViewListBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (MediaViewer_MediaList.SelectedItem == null)
            {
                MediaViewer_MediaPlayer.Source = null;
                return;
            }

            var selectedItem = MediaViewer_MediaList.SelectedItem as TaskItem;
            string mediaPath = Path.Combine(AppSettings.Main.InputDir, selectedItem.InputPath);
            if (selectedItem.State == TaskItemState.Done)
                mediaPath = Path.Combine(AppSettings.Main.OutputDir, selectedItem.OutputPath);

            MediaViewer_MediaPlayer.Source = new Uri(mediaPath, UriKind.Absolute);
            MediaViewer_MediaPlayer.Play();
        }
    }
}
