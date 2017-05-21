using AutoWaifu.Lib.Waifu2x;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AutoWaifu.Lib.Cui.WaifuCaffe
{
    [Serializable]
    public class WaifuCaffeOptions
    {
        public enum ImageNoiseLevel
        {
            None = 0,
            Low = 1,
            Medium = 2,
            High = 3
        }
        
        
        public WaifuConvertMode ConvertMode { get; set; } = WaifuConvertMode.CPU;
        public ProcessPriorityClass ProcessPriority { get; set; } = ProcessPriorityClass.Normal;


        internal int GpuIndex { get; set; } = 0;
        internal ImageNoiseLevel NoiseLevel { get; set; } = ImageNoiseLevel.Low;
        internal int BatchSize { get; set; } = 1;
        internal int CropSize { get; set; } = 128;



        public IResolutionResolver ResolutionResolver { get; set; }




        public string GetCuiParams(string inputFilePath, string outputFilePath)
        {
            List<string> paramParts = new List<string>();

            inputFilePath = Path.GetFullPath(inputFilePath);
            outputFilePath = Path.GetFullPath(outputFilePath);

            using (var img = Image.FromFile(inputFilePath))
            {
                paramParts.Add($"-i \"{inputFilePath}\"");
                var desiredResolution = ResolutionResolver.Resolve(new ImageResolution
                {
                    Width = img.Width,
                    Height = img.Height
                });

                int widthDifference = desiredResolution.WidthInt - img.Width;
                int heightDifference = desiredResolution.HeightInt - img.Height;

                double scale;

                if (widthDifference > heightDifference)
                    scale = desiredResolution.Width / img.Width;
                else
                    scale = desiredResolution.Height / img.Height;

                paramParts.Add($"-s {scale}");
            }

            paramParts.Add("-p");
            switch (ConvertMode)
            {
                case WaifuConvertMode.CPU:
                    paramParts.Add("cpu");
                    break;

                case WaifuConvertMode.GPU:
                    paramParts.Add("gpu");
                    paramParts.Add($"-gpu {GpuIndex}");
                    break;

                case WaifuConvertMode.cuDNN:
                    paramParts.Add("cudnn");
                    paramParts.Add($"-gpu {GpuIndex}");
                    break;
            }

            paramParts.Add($"-n {(int)NoiseLevel}");
            paramParts.Add($"-b {BatchSize}");
            paramParts.Add($"-c {CropSize}");


            paramParts.Add($"-o \"{outputFilePath}\"");

            return string.Join(" ", paramParts);
        }
    }
}
