using AutoWaifu.Lib.Waifu2x;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AutoWaifu.Lib.Prediction
{
    public class UpscaleIteration
    {
        public int IterationIndex { get; set; }

        public double UpscaleFactor { get; set; }

        public ImageResolution InputResolution { get; set; }

        public ImageResolution OutputResolution { get; set; }
    }
}
