using AutoWaifu.Lib.Waifu2x;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AutoWaifu.Lib.Prediction
{
    [Serializable]
    public class TaskConfiguration
    {

        public int InputImageWidth;
        public int InputImageHeight;

        public int OutputImageWidth;
        public int OutputImageHeight;

        public long NumInputPixels
        {
            get { return InputImageWidth * InputImageHeight; }
        }

        public long NumOutputPixels
        {
            get { return OutputImageWidth * OutputImageHeight; }
        }

        public WaifuImageType ImageType;

        public WaifuConvertMode ConvertMode;
    }
}
