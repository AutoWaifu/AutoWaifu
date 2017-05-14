using AutoWaifu.Lib.Waifu2x;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using PropertyChanged;
using System;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Xml;
using System.Xml.Serialization;

namespace AutoWaifu2
{
    [Serializable]
    public class AppSettings
    {
        static AppSettings _mainSettings;
        public static AppSettings Main
        {
            get
            {
                if (_mainSettings == null)
                    _mainSettings = new AppSettings();

                return _mainSettings;
            }
        }

        public static void SetMainSettings(AppSettings mainSettings)
        {
            _mainSettings = mainSettings;
        }
        
        [JsonConverter(typeof(StringEnumConverter))]
        public WaifuConvertMode ConversionMode { get; set; } = WaifuConvertMode.CPU;

        [JsonConverter(typeof(StringEnumConverter))]
        public ProcessPriorityClass Priority { get; set; } = ProcessPriorityClass.Idle;


        public String OutputDir { get; set; } = Path.GetFullPath("./Output");
        public String InputDir { get; set; } = Path.GetFullPath("./Input");
        public String Waifu2xCaffeDir { get; set; } = Path.GetFullPath("./waifu2x-caffe");
        public string FfmpegDir { get; set; } = Path.GetFullPath("./");
        public string TempDir { get; set; } = Path.GetFullPath("./tmp");

        public string TempDirInput { get { return Path.Combine(TempDir, "Input"); } }
        public string TempDirOutput { get { return Path.Combine(TempDir, "Output"); } }



        int _maxParallel = 2;
        public int MaxParallel
        {
            get => _maxParallel;
            set
            {
                _maxParallel = value;
                if (_maxParallel < 1)
                    _maxParallel = 1;
                if (_maxParallel > 8)
                    _maxParallel = 8;
            }
        }




        public MaxSizeResolutionResolver MaxSizeResolution { get; set; } = new MaxSizeResolutionResolver(new ImageResolution { Width = 3000, Height = 3000 });

        public MegapixelResolutionResolver MegapixelResolution { get; set; } = new MegapixelResolutionResolver(2000000.0f);

        public ScaleResolutionResolver ScaleResolution { get; set; } = new ScaleResolutionResolver(4.0f);


        public enum ResolutionResolverMode
        {
            MaxSize,
            TargetMegapixels,
            ScaleFactor
        }

        public ResolutionResolverMode ResolutionMode = ResolutionResolverMode.ScaleFactor;

        public AppSettings Copy()
        {
            return new AppSettings
            {
                ConversionMode = this.ConversionMode,
                Priority = this.Priority,

                Waifu2xCaffeDir = this.Waifu2xCaffeDir,
                InputDir = this.InputDir,
                OutputDir = this.OutputDir,

                MaxSizeResolution = this.MaxSizeResolution,
                MegapixelResolution = this.MegapixelResolution,
                ScaleResolution = this.ScaleResolution,

                ResolutionMode = this.ResolutionMode,

                MaxParallel = this.MaxParallel
            };
        }




        public static AppSettings LoadFromFile(String filePath)
        {
            return JsonConvert.DeserializeObject<AppSettings>(File.ReadAllText(filePath));
        }


        

        /// <returns>An XML representation of the app settings.</returns>
        public void SaveToFile(string filePath)
        {
            string json = JsonConvert.SerializeObject(this, Newtonsoft.Json.Formatting.Indented);
            File.WriteAllText(filePath, json);
        }
    }
}
