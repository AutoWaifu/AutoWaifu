using PropertyChanged;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace AutoWaifu.DataModel
{
    [ImplementPropertyChanged]
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

        public enum WaifuConvertMode
        {
            CPU,
            GPU,
            cuDNN
        }


        public WaifuConvertMode ConversionMode { get; set; } = WaifuConvertMode.CPU;
        public ProcessPriorityClass Priority { get; set; } = ProcessPriorityClass.Idle;

        
        public String OutputDir { get; set; }
        public String InputDir { get; set; }
        public String Waifu2xCaffeDir { get; set; } = null;
        public string TempDir { get; set; } = "tmp";

        public string TempDirInput { get { return Path.Combine(TempDir, "Input"); } }
        public string TempDirOutput { get { return Path.Combine(TempDir, "Output"); } }

        public bool UseScaleInsteadOfSize { get; set; } = false;
        public bool UseSizeInsteadOfScale { get { return !UseScaleInsteadOfSize; } }

        public int DesiredHeight { get; set; } = -1;
        public int DesiredWidth { get; set; } = -1;
        

        public int MaxParallel { get; set; } = 2;
        public float Scale { get; set; } = 3;
        



        public AppSettings Copy()
        {
            return new AppSettings
            {
                ConversionMode = this.ConversionMode,
                Priority = this.Priority,

                Waifu2xCaffeDir = this.Waifu2xCaffeDir,
                InputDir = this.InputDir,
                OutputDir = this.OutputDir,

                UseScaleInsteadOfSize = this.UseScaleInsteadOfSize,
                DesiredHeight = this.DesiredHeight,
                DesiredWidth = this.DesiredWidth,

                MaxParallel = this.MaxParallel,
                Scale = this.Scale,
            };
        }




        public void LoadFrom(String configFileContents)
        {
            var matchBool = @"[(?:true)(?:false)]+";
            var matchInt = @"[\d^\.]+";
            var matchFloat = @"[\d\.]+";
            string matchString = @"[^\""\<\>\|\?\*\n\t\r]+";



            UseScaleInsteadOfSize = TryParse(configFileContents, "useRequestedSize", matchBool, UseScaleInsteadOfSize);
            DesiredHeight =         TryParse(configFileContents, "desiredHeight", matchInt, DesiredHeight);
            DesiredWidth =          TryParse(configFileContents, "desiredWidth", matchInt, DesiredWidth);
            Waifu2xCaffeDir =   TryParse(configFileContents, "waifuCaffePath", matchString, Waifu2xCaffeDir);
            OutputDir =         TryParse(configFileContents, "outDir", matchString, OutputDir);
            InputDir =          TryParse(configFileContents, "inDir", matchString, InputDir);
            MaxParallel =       TryParse(configFileContents, "maxParallel", matchInt, MaxParallel);
            Scale =             TryParse(configFileContents, "scale", matchFloat, Scale);


            ConversionMode =    TryParseEnum(configFileContents, "convertMode", matchString, ConversionMode);
            Priority =          TryParseEnum(configFileContents, "priority", matchString, Priority);
        }



        class SettingsFileEntry
        {
            public string Key;
            public string Value;
            public string Line;
        }

        /// <returns>A copy of configFileContents with values added/updated from this object.</returns>
        public string SaveTo(String configFileContents)
        {
            //  TODO

            string matchValidLine = @"\s*[^//]";

            var validLines = configFileContents.Split(new string[] { Environment.NewLine, "\n", "\r" }, StringSplitOptions.RemoveEmptyEntries)
                                               .Where(line => Regex.IsMatch(line, matchValidLine))
                                               .Where(line => line.Contains("="))
                                               .ToList();

            var namedValues = validLines.ToDictionary(
                                            (line) => line.Split('=')[0].Trim(),
                                            (line) =>
                                                new SettingsFileEntry
                                                {
                                                    Key = line.Split('=')[0].Trim(),
                                                    Value = line.Split('=')[1].Trim(),
                                                    Line = line
                                                }
                                        );

            Func<string, string, object, string> UpdateConfigFile = (config, key, value) =>
            {
                if (!namedValues.ContainsKey(key))
                {
                    config += $"{Environment.NewLine}{key} = {value}{Environment.NewLine}";
                }
                else
                {
                    switch (key)
                    {
                        case "outDir":
                            string path = namedValues[key].Value;
                            if (path == null || path.Trim().Length == 0)
                                path = ".";
                            namedValues[key].Value = Path.GetFullPath(path);
                            break;

                        case "inDir":
                            goto case "outDir";

                        case "waifuCaffePath":
                            goto case "outDir";
                    }

                    if (namedValues[key].Value.ToLower() != value.ToString().ToLower())
                        config = config.Replace(namedValues[key].Line, $"{key} = {value}");
                }

                return config;
            };

            string result = configFileContents;

            result = UpdateConfigFile(result, "useRequestedSize", this.UseScaleInsteadOfSize);
            result = UpdateConfigFile(result, "desiredWidth", this.DesiredWidth);
            result = UpdateConfigFile(result, "desiredHeight", this.DesiredHeight);
            result = UpdateConfigFile(result, "waifuCaffePath", this.Waifu2xCaffeDir);
            result = UpdateConfigFile(result, "outDir", this.OutputDir);
            result = UpdateConfigFile(result, "inDir", this.InputDir);
            result = UpdateConfigFile(result, "maxParallel", this.MaxParallel);
            result = UpdateConfigFile(result, "scale", this.Scale);

            result = UpdateConfigFile(result, "convertMode", this.ConversionMode);
            result = UpdateConfigFile(result, "priority", this.Priority);



            return result;
        }

        public string Validate()
        {
            if (Waifu2xCaffeDir?.Trim().Length == 0)
                return "Can't find Waifu2x-Caffe folder!";

            

            return null;
        }

        bool TryParse(string file, string propName, string typeMatch, bool defaultVal)
        {
            string valueText = Parse(file, propName, typeMatch);
            if (valueText == null)
                return defaultVal;

            return bool.Parse(valueText);
        }

        int TryParse(string file, string propName, string typeMatch, int defaultVal)
        {
            string valueText = Parse(file, propName, typeMatch);
            if (valueText == null)
                return defaultVal;

            return int.Parse(valueText);
        }

        float TryParse(string file, string propName, string typeMatch, float defaultVal)
        {
            string valueText = Parse(file, propName, typeMatch);
            if (valueText == null)
                return defaultVal;

            return float.Parse(valueText);
        }

        string TryParse(string file, string propName, string typeMatch, string defaultVal)
        {
            string valueText = Parse(file, propName, typeMatch);
            if (valueText == null)
                return defaultVal;

            return valueText;
        }

        T TryParseEnum<T>(string file, string propName, string typeMatch, T defaultVal)
        {
            string valueText = Parse(file, propName, typeMatch);
            if (valueText == null)
                return defaultVal;

            return (T)Enum.Parse(typeof(T), valueText);
        }

        string Parse(String file, string propName, string typeMatch)
        {
            Regex regex;
            regex = new Regex("^" + propName + @"\s*=\s*(" + typeMatch + @")\s*$", RegexOptions.Multiline | RegexOptions.IgnoreCase);
            var match = regex.Match(file);
            if (match == null || match.Groups == null || match.Groups.Count < 2)
                return null;

            return match.Groups[1].Value.Trim();
        }
    }
}
