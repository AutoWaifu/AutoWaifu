using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AutoWaifu.Lib.Cui.Ffmpeg
{
    [Serializable]
    public abstract class IFfmpegOptions
    {
        public abstract string GetCuiParams(string inputImageNameFormat, string outputFilePath);

        int outputFrameRate = -1;
        public int OutputFramerate
        {
            get => this.outputFrameRate;
            set
            {
                this.outputFrameRate = value;

                if (this.outputFrameRate < 1)
                    this.outputFrameRate = 1;

                if (this.outputFrameRate > 120)
                    this.outputFrameRate = 120;
            }
        }
    }
}
