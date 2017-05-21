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
    [ImplementPropertyChanged]
    public class AppSettingsViewModel
    {
        public AppSettingsViewModel(AppSettings model)
        {
            this.Model = model;
        }

        public AppSettings Model { get; set; }

        public Visibility VisibilityMaxSizeResolutionMode { get { return ResolutionMode == ResolutionResolverMode.MaxSize ? Visibility.Visible : Visibility.Hidden; } }
        public Visibility VisibilityMegapixelResolutionMode { get { return ResolutionMode == ResolutionResolverMode.TargetMegapixels ? Visibility.Visible : Visibility.Hidden; } }
        public Visibility VisibilityScaleFactorMode { get { return ResolutionMode == ResolutionResolverMode.ScaleFactor ? Visibility.Visible : Visibility.Hidden; } }



        public ResolutionResolverMode ResolutionMode
        {
            get { return Model.ResolutionMode; }
            set { Model.ResolutionMode = value; }
        }

        public WaifuConvertMode ConversionMode
        {
            get { return Model.ConversionMode; }
            set { Model.ConversionMode = value; }
        }

        public ProcessPriorityClass ProcessPriority
        {
            get { return Model.Priority; }
            set { Model.Priority = value; }
        }

        public string InputDir
        {
            get { return Model.InputDir; }
            set { Model.InputDir = value; }
        }

        public string OutputDir
        {
            get { return Model.OutputDir; }
            set { Model.OutputDir = value; }
        }

        public string TempDir
        {
            get { return Model.TempDir; }
            set { Model.TempDir = value; }
        }

        public string Waifu2xCaffeDir
        {
            get { return Model.Waifu2xCaffeDir; }
            set { Model.Waifu2xCaffeDir = value; }
        }

        public string FfmpegDir
        {
            get { return Model.FfmpegDir; }
            set { Model.FfmpegDir = value; }
        }

        public int MaxParallel
        {
            get { return Model.MaxParallel; }
            set { Model.MaxParallel = value; }
        }
    }
}
