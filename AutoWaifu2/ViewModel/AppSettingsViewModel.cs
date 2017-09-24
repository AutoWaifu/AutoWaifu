using AutoWaifu.Lib.Cui.Ffmpeg;
using AutoWaifu.Lib.Waifu2x;
using PropertyChanged;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using static AutoWaifu2.AppSettings;

namespace AutoWaifu2
{
    [AddINotifyPropertyChangedInterface]
    public class AppSettingsViewModel
    {
        public AppSettingsViewModel()
        {
            this.Model = new AppSettings();
        }

        public AppSettingsViewModel(AppSettings model)
        {
            this.Model = model;
        }

        [Browsable(false)]
        public AppSettings Model { get; set; }

        [Browsable(false)]
        public Visibility VisibilityMaxSizeResolutionMode
        {
            get => ResolutionMode == ResolutionResolverMode.MaxSize ? Visibility.Visible : Visibility.Hidden;
            set { }
        }

        [Browsable(false)]
        public Visibility VisibilityMegapixelResolutionMode
        {
            get => ResolutionMode == ResolutionResolverMode.TargetMegapixels ? Visibility.Visible : Visibility.Hidden;
            set { }
        }

        [Browsable(false)]
        public Visibility VisibilityScaleFactorMode
        {
            get => ResolutionMode == ResolutionResolverMode.ScaleFactor ? Visibility.Visible : Visibility.Hidden;
            set { }
        }

        [Browsable(false)]
        public Visibility VisibilityCompatibilityMode
        {
            get => FfmpegConvertMode == FfmpegConvertModeEnum.Compatibility ? Visibility.Visible : Visibility.Hidden;
            set { }
        }

        [Browsable(false)]
        public Visibility VisibilityCrfMode
        {
            get => FfmpegConvertMode == FfmpegConvertModeEnum.CRF ? Visibility.Visible : Visibility.Hidden;
            set { }
        }





        public bool AutoStartOnOpen
        {
            get => Model.AutoStartOnOpen;
            set => Model.AutoStartOnOpen = value;
        }


        [Browsable(false)]
        public ResolutionResolverMode ResolutionMode
        {
            get => Model.ResolutionMode;
            set => Model.ResolutionMode = value;
        }

        [Browsable(false)]
        public WaifuConvertMode ConversionMode
        {
            get => Model.ConversionMode;
            set => Model.ConversionMode = value;
        }

        [Browsable(false)]
        public FfmpegConvertModeEnum FfmpegConvertMode
        {
            get => Model.FfmpegConvertMode;
            set => Model.FfmpegConvertMode = value;
        }

        [Browsable(false)]
        public ProcessPriorityClass ProcessPriority
        {
            get => Model.Priority;
            set => Model.Priority = value;
        }

        [Browsable(false)]
        public string InputDir
        {
            get => Model.InputDir;
            set => Model.InputDir = value;
        }

        [Browsable(false)]
        public string OutputDir
        {
            get => Model.OutputDir;
            set => Model.OutputDir = value;
        }
        
        public string TempDir
        {
            get => Model.TempDir;
            set => Model.TempDir = value;
        }

        [Browsable(false)]
        public string Waifu2xCaffeDir
        {
            get => Model.Waifu2xCaffeDir;
            set => Model.Waifu2xCaffeDir = value;
        }

        [Browsable(false)]
        public string FfmpegDir
        {
            get => Model.FfmpegDir;
            set => Model.FfmpegDir = value;
        }

        [Browsable(false)]
        public int MaxParallel
        {
            get => Model.MaxParallel;
            set => Model.MaxParallel = value;
        }

        [Category("Denoise")]
        public int ImageDenoiseAmoint
        {
            get => Model.ImageDenoiseAmount;
            set => Model.ImageDenoiseAmount = value;
        }

        [Category("Denoise")]
        public int GifDenoiseAmount
        {
            get => Model.GifDenoiseAmount;
            set => Model.GifDenoiseAmount = value;
        }
        
        public ImportFileMethod FileDragMethod
        {
            get => Model.FileDragMethod;
            set => Model.FileDragMethod = value; 
        }

        [Browsable(false)]
        public FfmpegCompatibilityOptions FfmpegCompatibility
        {
            get => Model.FfmpegCompatibility;
            set => Model.FfmpegCompatibility = value;
        }

        [Browsable(false)]
        public FfmpegCrfEncodingOptions FfmpegCrf
        {
            get => Model.FfmpegCrf;
            set => Model.FfmpegCrf = value;
        }

        [Category("Animation Output")]
        public AnimationOutputMode GifOutputType
        {
            get => Model.GifOutputType;
            set => Model.GifOutputType = value;
        }

        [Category("Animation Output")]
        public AnimationOutputMode VideoOutputType
        {
            get => Model.VideoOutputType;
            set => Model.VideoOutputType = value;
        }

        public string IgnoredFileExtensions
        {
            get => Model.IgnoredFilesFilter;
            set => Model.IgnoredFilesFilter = value;
        }
    }
}
