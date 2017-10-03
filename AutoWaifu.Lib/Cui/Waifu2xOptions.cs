using AutoWaifu.Lib.Waifu2x;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AutoWaifu.Lib.Cui
{
    [Serializable]
    public class Waifu2xOptions
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
        internal ImageNoiseLevel NoiseLevel { get; set; } = ImageNoiseLevel.None;
        internal int BatchSize { get; set; } = 1;



        public IResolutionResolver OutputResolutionResolver { get; set; }


        internal double GetOutputScale(string inputFilePath)
        {
            using (var img = Image.FromFile(inputFilePath))
            {
                var desiredResolution = OutputResolutionResolver.Resolve(new ImageResolution
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

                return scale;
            }
        }
    }
}
