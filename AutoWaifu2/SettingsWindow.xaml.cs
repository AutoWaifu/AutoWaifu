using AutoWaifu.Lib.Cui.Ffmpeg;
using AutoWaifu.Lib.Waifu2x;
using Serilog;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net.Http;
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
using static AutoWaifu.Lib.Cui.Ffmpeg.FfmpegCompatibilityOptions;
using static AutoWaifu2.AppSettings;

namespace AutoWaifu2
{
    /// <summary>
    /// Interaction logic for SettingsPage.xaml
    /// </summary>
    public partial class SettingsWindow : Window
    {
        ILogger Logger = Log.ForContext<SettingsWindow>();

        Dictionary<string, ResolutionResolverMode> StringResizeModeMap = new Dictionary<string, ResolutionResolverMode>
        {
            { "Max Size", ResolutionResolverMode.MaxSize },
            { "Scale Factor", ResolutionResolverMode.ScaleFactor },
            { "Target Resolution", ResolutionResolverMode.TargetMegapixels }
        };

        Dictionary<string, ProcessPriorityClass> StringProcessPriorityMap = new Dictionary<string, ProcessPriorityClass>
        {
            { "Idle", ProcessPriorityClass.Idle },
            { "Below Normal", ProcessPriorityClass.BelowNormal },
            { "Normal", ProcessPriorityClass.Normal },
            { "Above Normal", ProcessPriorityClass.AboveNormal },
            { "High", ProcessPriorityClass.High },
            { "Realtime", ProcessPriorityClass.RealTime }
        };

        Dictionary<string, WaifuConvertMode> StringConvertModeMap = new Dictionary<string, WaifuConvertMode>
        {
            { "CPU", WaifuConvertMode.CPU },
            { "GPU (CUDA)", WaifuConvertMode.GPU },
            { "cuDNN", WaifuConvertMode.cuDNN }
        };

        Dictionary<string, FfmpegConvertModeEnum> StringGifModeMap = new Dictionary<string, FfmpegConvertModeEnum>
        {
            { "Compatibility", FfmpegConvertModeEnum.Compatibility },
            { "Constant Rate Factor", FfmpegConvertModeEnum.CRF }
        };

        Dictionary<string, OutputCompatibilityType> StringGifCompatibilityModeMap = new Dictionary<string, OutputCompatibilityType>
        {
            { "High Quality, Low Compatibility", OutputCompatibilityType.HighQualityLowCompatibility },
            { "Good Quality, Medium Compatibility", OutputCompatibilityType.GoodQualityMediumCompatibility },
            { "Low Quality, Best Compatibility", OutputCompatibilityType.LowQualityBestCompatibility }
        };

        public SettingsWindow()
        {
            InitializeComponent();

            Res_CbResMode.ExtSetMappings(StringResizeModeMap);

            Process_MethodCbx.ExtSetMappings(StringConvertModeMap);

            Process_PriorityCbx.ExtSetMappings(StringProcessPriorityMap);

            //Process_CbGifMode.ExtSetMappings(StringGifModeMap);
            //Process_CbGifCompatibilityMode.ExtSetMappings(StringGifCompatibilityModeMap);



            InputFolderPathInput.InputPathType = PathInput.PathType.Folder;
            OutputFolderPathInput.InputPathType = PathInput.PathType.Folder;
            WaifuFolderPathInput.InputPathType = PathInput.PathType.Folder;
            FfmpegFilePathInput.InputPathType = PathInput.PathType.Folder;






            this.Closing += SettingsWindow_Closing;
        }

        private void SettingsWindow_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            if (!DialogResult.HasValue)
                DialogResult = false;

            if (DialogResult.Value)
            {
                var inputDir = InputFolderPathInput.Value;
                var outputDir = OutputFolderPathInput.Value;
                var waifuDir = WaifuFolderPathInput.Value;
                var ffmpegDir = FfmpegFilePathInput.Value;

                if (!Directory.Exists(inputDir))
                {
                    MessageBox.Show($"The input folder '{inputDir}' doesn't exist!");
                    e.Cancel = true;
                }

                if (!Directory.Exists(outputDir))
                {
                    MessageBox.Show($"The output folder '{outputDir}' doesn't exist!");
                    e.Cancel = true;
                }

                if (!File.Exists(Path.Combine(waifuDir, "waifu2x-caffe-cui.exe")))
                {
                    MessageBox.Show($"The waifu2x-caffe folder '{waifuDir}' doesn't contain waifu2x-caffe-cui.exe!");
                    e.Cancel = true;
                }

                if (!File.Exists(Path.Combine(ffmpegDir, "ffmpeg.exe")))
                {
                    MessageBox.Show($"The ffmpeg folder '{ffmpegDir}' doesn't contain ffmpeg.exe!");
                    e.Cancel = true;
                }

                

                if (!e.Cancel)
                {
                    ViewModel.InputDir = inputDir;
                    ViewModel.OutputDir = outputDir;
                    ViewModel.Waifu2xCaffeDir = waifuDir;
                    ViewModel.FfmpegDir = ffmpegDir;
                }
            }
        }

        public AppSettingsViewModel ViewModel
        {
            get { return this.DataContext as AppSettingsViewModel; }
            set
            {
                Res_CbResMode.ExtSetValue(value.ResolutionMode);
                Process_MethodCbx.ExtSetValue(value.ConversionMode);
                Process_PriorityCbx.ExtSetValue(value.ProcessPriority);

                //Process_CbGifMode.ExtSetValue(value.GifMode);
                //Process_CbGifCompatibilityMode.ExtSetValue(value.Model.FfmpegCompatibility.TargetCompatibility);
                

                InputFolderPathInput.Value = value.InputDir;
                OutputFolderPathInput.Value = value.OutputDir;
                WaifuFolderPathInput.Value = value.Waifu2xCaffeDir;
                FfmpegFilePathInput.Value = value.FfmpegDir;

                //Process_ThreadCountIud.Value = value.MaxParallel;

                //DataPropertyGrid.DataContext = value;

                this.DataContext = value;
            }
        }





        private void Process_MethodCbx_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (ViewModel != null)
                ViewModel.ConversionMode = Process_MethodCbx.ExtGetSelectedValue<WaifuConvertMode>();
        }

        private void Process_PriorityCbx_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (ViewModel != null)
                ViewModel.ProcessPriority = Process_PriorityCbx.ExtGetSelectedValue<ProcessPriorityClass>();
        }






        private void Res_CbResMode_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (ViewModel != null)
                ViewModel.ResolutionMode = Res_CbResMode.ExtGetSelectedValue<ResolutionResolverMode>();
        }

        private void OkButton_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = true;
        }

        private void CancelButton_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = false;
        }

        //private void Process_CbGifCompatibilityMode_SelectionChanged(object sender, SelectionChangedEventArgs e)
        //{
        //    if (ViewModel != null)
        //        ViewModel.Model.FfmpegCompatibility.TargetCompatibility = Process_CbGifCompatibilityMode.ExtGetSelectedValue<OutputCompatibilityType>();
        //}

        //private async void CheckForUpdatesButton_Click(object sender, RoutedEventArgs e)
        //{
        //    string updateCheckUrl = ViewModel.Model.UpdateVersionCheckUrl;
        //    var httpClient = new HttpClient();

        //    Logger.Debug("Checking for updates at {@UpdateCheckUrl}", updateCheckUrl);
        //    var latestVersionRequestMessage = await httpClient.GetAsync(updateCheckUrl);
        //    if (!latestVersionRequestMessage.IsSuccessStatusCode)
        //    {
        //        MessageBox.Show(this, $"Sorry, could not check for updates! ({latestVersionRequestMessage.ReasonPhrase})");
        //        Logger.Warning("Failed to GET {@UpdateCheckUrl}, error {@StatusCode} - {@ReasonPhrase}", updateCheckUrl, latestVersionRequestMessage.StatusCode, latestVersionRequestMessage.ReasonPhrase);
        //        return;
        //    }

        //    string file = await latestVersionRequestMessage.Content.ReadAsStringAsync();
        //    var lines = file.Split('\n');

        //    if (lines.Length < 2)
        //    {
        //        MessageBox.Show("The update file is invalid, contact the developer!");
        //        return;
        //    }

        //    string version = lines[0].Trim();
        //    string latestVersionUrl = lines[1].Trim();

        //    if (version == RootConfig.CurrentVersion)
        //    {
        //        MessageBox.Show($"You've got the latest version of AutoWaifu! (v{version})");
        //    }
        //    else
        //    {
        //        var shouldDownload = MessageBox.Show($"AutoWaifu v{version} is available at {latestVersionUrl}, would you like to download it?", "", MessageBoxButton.YesNo);
        //        if (shouldDownload == MessageBoxResult.Yes)
        //        {
        //            var latestBinaryMessage = await httpClient.GetAsync(latestVersionUrl);
        //            if (!latestBinaryMessage.IsSuccessStatusCode)
        //            {
        //                MessageBox.Show(this, $"Sorry, AutoWaifu version {version} couldn't be downloaded. ({latestBinaryMessage.ReasonPhrase})");
        //                Logger.Warning("Failed to GET {@VersionUrl}, error {@StatusCode} - {@ReasonPhrase}", latestVersionUrl, latestBinaryMessage.StatusCode, latestBinaryMessage.ReasonPhrase);
        //                return;
        //            }
        //            else
        //            {
        //                byte[] latestBinary = await latestBinaryMessage.Content.ReadAsByteArrayAsync();
        //                string fileName = Path.GetFileName(latestVersionUrl);

        //                File.WriteAllBytes(fileName, latestBinary);

        //                MessageBox.Show($"v{version} has been downloaded to AutoWaifu's current folder as {fileName}. Please close AutoWaifu and open the downloaded version.");
        //            }
        //        }
        //    }
        //}
    }
}
