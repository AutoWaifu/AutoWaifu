using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AutoWaifu.Lib.Cui.Ffmpeg
{
    public class FfmpegRawOptions : IFfmpegOptions
    {
        public string RawParams { get; set; }

        public override string GetCuiParams(string inputImageNameFormat, string outputFilePath)
        {
            return RawParams;
        }
    }
}
