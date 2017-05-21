using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AutoWaifu.Lib.Cui.Ffmpeg
{
    [Serializable]
    class FfmpegCrfEncodingOptions : IFfmpegOptions
    {
        int crf = 25;
        public int CRF
        {
            get => this.crf;
            set
            {
                this.crf = value;

                if (this.crf < 0)
                    this.crf = 0;
                if (this.crf > 50)
                    this.crf = 50;
            }
        }

        public override string GetCuiParams(string inputImageNameFormat, string outputFilePath)
        {
            List<string> paramParts = new List<string>();
            paramParts.Add($"-r {OutputFramerate}");

            paramParts.Add($"-i \"{inputImageNameFormat}\"");

            paramParts.Add("-vcodec h264");

            paramParts.Add($"-crf {CRF}");

            paramParts.Add("-movflags +faststart");

            paramParts.Add($"\"{outputFilePath}\"");

            return string.Join(" ", paramParts);
        }
    }
}
