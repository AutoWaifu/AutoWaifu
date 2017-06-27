using AutoWaifu.Lib.Waifu2x;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AutoWaifu.Lib.Prediction
{
    public class CompletedTaskMetric
    {
        public List<UpscaleIteration> UpscaleIterations { get; set; } = new List<UpscaleIteration>();

        /// <summary>
        /// % total CPU usage dedicated to this task
        /// </summary>
        public double TotalProcessorTime { get; set; }

        public DateTime StartTime { get; set; }
        public DateTime EndTime { get; set; }

        public WaifuConvertMode ConversionMode { get; set; }
    }
}
