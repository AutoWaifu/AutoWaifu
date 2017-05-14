using AutoWaifu.Lib.Waifu2x;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
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
        public MainWindow()
        {
            InitializeComponent();

            this.Closing += MainWindow_Closing;

            ViewModel = new MainWindowViewModel();
            ViewModel.Initialize(this.Dispatcher);
            ViewModel.StartProcessing();
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
            var settingsCopy = ViewModel.Settings.Copy();
            settingsWindow.ViewModel = new AppSettingsViewModel(settingsCopy);

            if (settingsWindow.ShowDialog().Value)
            {
                ViewModel.Settings = settingsCopy;
                ViewModel.Settings.SaveToFile(RootConfig.SettingsFilePath);
            }
        }

        private void OutputCtxMenuRestartItem_Click(object sender, RoutedEventArgs e)
        {
            if (MessageBoxResult.Yes != MessageBox.Show("Are you sure you want to restart these tasks? The current output files will be deleted.", "", MessageBoxButton.YesNo))
                return;

            var filesToRestart = OutputFilesList.SelectedItems.Cast<string>();
            var items = filesToRestart.Select((f) => ViewModel.TaskItems[f]);
            foreach (var item in items)
                File.Delete(item.OutputPath);
        }

        private void OutputCtxMenuOpenOutputFolder_Click(object sender, RoutedEventArgs e)
        {
            var selectedFile = OutputFilesList.SelectedItem as string;
            if (selectedFile == null)
                return;

            var task = ViewModel.TaskItems[selectedFile];

            ProcessStartInfo psi = new ProcessStartInfo();
            psi.FileName = "explorer";
            psi.Arguments = $"/select, \"{task.OutputPath}\"";

            Process.Start(psi);
        }

    }
}
