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
using static AutoWaifu2.AppSettings;

namespace AutoWaifu2
{
    /// <summary>
    /// Interaction logic for SettingsPage.xaml
    /// </summary>
    public partial class SettingsWindow : Window
    {

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

        public SettingsWindow()
        {
            InitializeComponent();

            List<string> resOptionValues = new List<string>();
            foreach (var resOption in StringResizeModeMap)
                resOptionValues.Add(resOption.Key);

            Res_CbResMode.ItemsSource = resOptionValues;
            Res_CbResMode.SelectedIndex = 0;



            List<string> convertOptionValues = new List<string>();
            foreach (var convertOpt in StringConvertModeMap)
                convertOptionValues.Add(convertOpt.Key);

            Process_MethodCbx.ItemsSource = convertOptionValues;
            Process_MethodCbx.SelectedIndex = 0;




            List<string> priorityOptionValues = new List<string>();
            foreach (var priorityOpt in StringProcessPriorityMap)
                priorityOptionValues.Add(priorityOpt.Key);

            Process_PriorityCbx.ItemsSource = priorityOptionValues;
            Process_PriorityCbx.SelectedIndex = 0;






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
                var newResMode = value.ResolutionMode;
                var currentResMode = StringResizeModeMap[Res_CbResMode.SelectedValue as string];

                if (newResMode != currentResMode)
                    Res_CbResMode.SelectedIndex = StringResizeModeMap.Values.ToList().IndexOf(newResMode);



                var newConvertMode = value.ConversionMode;
                var currentConvertMode = StringConvertModeMap[Process_MethodCbx.SelectedValue as string];

                if (newConvertMode != currentConvertMode)
                    Process_MethodCbx.SelectedIndex = StringConvertModeMap.Values.ToList().IndexOf(newConvertMode);



                InputFolderPathInput.Value = value.InputDir;
                OutputFolderPathInput.Value = value.OutputDir;
                WaifuFolderPathInput.Value = value.Waifu2xCaffeDir;
                FfmpegFilePathInput.Value = value.FfmpegDir;

                //Process_ThreadCountIud.Value = value.MaxParallel;

                this.DataContext = value;
            }
        }





        private void Process_MethodCbx_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (ViewModel == null)
                return;

            var methodType = StringConvertModeMap[Process_MethodCbx.SelectedValue as string];
            if (methodType != ViewModel.ConversionMode)
                ViewModel.ConversionMode = methodType;
        }

        private void Process_PriorityCbx_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (ViewModel == null)
                return;

            var priorityType = StringProcessPriorityMap[Process_PriorityCbx.SelectedValue as string];
            if (priorityType != ViewModel.ProcessPriority)
                ViewModel.ProcessPriority = priorityType;
        }






        private void Res_CbResMode_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (ViewModel == null)
                return;

            var resType = StringResizeModeMap[Res_CbResMode.SelectedValue as string];
            if (resType != ViewModel.ResolutionMode)
                ViewModel.ResolutionMode = resType;
        }

        private void OkButton_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = true;
        }

        private void CancelButton_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = false;
        }


    }
}
