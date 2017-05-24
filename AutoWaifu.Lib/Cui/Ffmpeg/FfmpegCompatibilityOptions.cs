using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AutoWaifu.Lib.Cui.Ffmpeg
{
    [Serializable]
    class FfmpegCompatibilityOptions : IFfmpegOptions
    {
        public enum OutputCompatibilityType
        {
            HighQualityLowCompatibility,
            GoodQualityMediumCompatibility,
            LowQualityBestCompatibility
        }

        public OutputCompatibilityType TargetCompatibility { get; set; } = OutputCompatibilityType.GoodQualityMediumCompatibility;


        public override string GetCuiParams(string inputImageNameFormat, string outputFilePath)
        {
            List<string> paramParts = new List<string>();

            if (Path.GetExtension(inputImageNameFormat).ToLower() == ".png")
                paramParts.Add($"-framerate {this.OutputFramerate}");
            else
                paramParts.Add($"-r {this.OutputFramerate}");

            paramParts.Add($"-i \"{inputImageNameFormat}\"");

            paramParts.Add("-vcodec h264");

            switch (TargetCompatibility)
            {
                case OutputCompatibilityType.LowQualityBestCompatibility:
                    paramParts.Add("-crf 30");
                    paramParts.Add("-profile:v baseline -level 3.0");
                    paramParts.Add("-pix_fmt yuv420p");
                    break;

                case OutputCompatibilityType.GoodQualityMediumCompatibility:
                    paramParts.Add("-crf 22");
                    paramParts.Add("-profile:v main -level 3.1");
                    paramParts.Add("-pix_fmt yuv420p");
                    break;

                case OutputCompatibilityType.HighQualityLowCompatibility:
                    paramParts.Add("-crf 10");
                    paramParts.Add("-profile:v high -level 4.0");
                    paramParts.Add("-pix_fmt yuv420p");
                    break;
            }

            paramParts.Add("-movflags +faststart");

            paramParts.Add($"\"{outputFilePath}\"");

            return string.Join(" ", paramParts);
        }
    }
}
