using AutoWaifu.Lib.Cui.Ffmpeg;
using AutoWaifu.Lib.Waifu2x;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using PropertyChanged;
using Serilog;
using System;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Runtime.Serialization.Formatters.Binary;
using System.Xml;
using System.Xml.Serialization;

namespace AutoWaifu2
{
    [Serializable]
    public class AppSettings
    {
        ILogger Logger = Log.ForContext<AppSettings>();

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



        int _gifDenoiseAmt = 1;
        public int GifDenoiseAmount
        {
            get => _gifDenoiseAmt;
            set
            {
                _gifDenoiseAmt = value;

                if (_gifDenoiseAmt < 0)
                    _gifDenoiseAmt = 0;

                if (_gifDenoiseAmt > 10)
                    _gifDenoiseAmt = 10;
            }
        }



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




        public enum ImportFileMethod
        {
            Copy,
            Move
        }

        [JsonConverter(typeof(StringEnumConverter))]
        public ImportFileMethod FileDragMethod { get; set; } = ImportFileMethod.Copy;


        public bool AutoStartOnOpen { get; set; } = true;

        public MaxSizeResolutionResolver MaxSizeResolution { get; set; } = new MaxSizeResolutionResolver(new ImageResolution { Width = 3000, Height = 3000 });

        public MegapixelResolutionResolver MegapixelResolution { get; set; } = new MegapixelResolutionResolver(2000000.0f);

        public ScaleResolutionResolver ScaleResolution { get; set; } = new ScaleResolutionResolver(4.0f);


        public enum ResolutionResolverMode
        {
            MaxSize,
            TargetMegapixels,
            ScaleFactor
        }

        [JsonConverter(typeof(StringEnumConverter))]
        public ResolutionResolverMode ResolutionMode { get; set; } = ResolutionResolverMode.ScaleFactor;



        public string UpdateVersionCheckUrl { get; set; } = "http://autowaifu.azurewebsites.net/update.txt";



        public FfmpegCompatibilityOptions FfmpegCompatibility { get; set; } = new FfmpegCompatibilityOptions { TargetCompatibility = FfmpegCompatibilityOptions.OutputCompatibilityType.GoodQualityMediumCompatibility };

        public FfmpegCrfEncodingOptions FfmpegCrf { get; set; } = new FfmpegCrfEncodingOptions { CRF = 30 };

        public enum AnimationConvertMode
        {
            Compatibility,
            CRF
        }

        [JsonConverter(typeof(StringEnumConverter))]
        public AnimationConvertMode AnimationMode { get; set; } = AnimationConvertMode.Compatibility;



        public enum GifAnimationExtractionMode
        {
            ImageMagick,
            Ffmpeg
        }

        [JsonConverter(typeof(StringEnumConverter))]
        public GifAnimationExtractionMode GifFrameExtractionMode { get; set; } = GifAnimationExtractionMode.Ffmpeg;



        public enum AnimationOutputMode
        {
            GIF,
            MP4
        }

        [JsonConverter(typeof(StringEnumConverter))]
        public AnimationOutputMode GifOutputType { get; set; } = AnimationOutputMode.MP4;

        [JsonConverter(typeof(StringEnumConverter))]
        public AnimationOutputMode VideoOutputType { get; set; } = AnimationOutputMode.MP4;
        

        public bool ParallelizeAnimationConversion { get; set; } = false;

        float _animationParallelizationMaxThreadsFactor = 0.8f;
        public float AnimationParallelizationMaxThreadsFactor
        {
            get => this._animationParallelizationMaxThreadsFactor;
            set
            {
                this._animationParallelizationMaxThreadsFactor = value;

                if (this._animationParallelizationMaxThreadsFactor < 0.1f)
                    this._animationParallelizationMaxThreadsFactor = 0.1f;
                if (this._animationParallelizationMaxThreadsFactor > 1.0f)
                    this._animationParallelizationMaxThreadsFactor = 1.0f;
            }
        }



        T DeepCopy<T>(T value) where T : class, new()
        {
            string json = JsonConvert.SerializeObject(value);
            return JsonConvert.DeserializeObject<T>(json);
        }

        public AppSettings Copy()
        {
            return DeepCopy(this);
        }




        public static AppSettings LoadFromFile(String filePath)
        {
            var logger = Log.ForContext<AppSettings>();
            logger.Debug("Loading AppSettings from '{@SettingsFilePath}'", filePath);
            return JsonConvert.DeserializeObject<AppSettings>(File.ReadAllText(filePath));
        }


        

        /// <returns>An XML representation of the app settings.</returns>
        public void SaveToFile(string filePath)
        {
            Logger.Debug("Saving AppSettings to '{@SettingsFilePath}'", filePath);
            string json = JsonConvert.SerializeObject(this, Newtonsoft.Json.Formatting.Indented);
            File.WriteAllText(filePath, json);
        }
    }
}
