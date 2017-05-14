using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

namespace AutoWaifu.Lib.Prediction
{
    [Serializable]
    public class TimePoint
    {
        [DllImport("kernel32.dll")]
        [return: MarshalAs(UnmanagedType.Bool)]
        static extern bool QueryProcessCycleTime(IntPtr ProcessHandle, out ulong CycleTime);

        public ulong ProcessingCycles;
        public DateTime StartTime;
        public DateTime EndTime;
        //  TODO - Take CPU cycles to complete into account (cycles/perscalepixel)
        

        public TaskConfiguration TaskConfig;



        public long NumGeneratedPixels
        {
            get { return TaskConfig.NumOutputPixels - TaskConfig.NumInputPixels; }
        }

        public double CyclesPerGeneratedPixel
        {
            get
            {
                return ProcessingCycles / (double)(TaskConfig.NumOutputPixels - TaskConfig.NumInputPixels);
            }
        }
        
        public double CyclesPerInputPixel
        {
            get { return ProcessingCycles / (double)(TaskConfig.NumInputPixels); }
        }
        public double CyclesPerOutputPixel
        {
            get { return ProcessingCycles / (double)(TaskConfig.NumOutputPixels); }
        }



        //  TODO - Make use of this
        public int NumUpscaleIterations
        {
            get
            {
                double scale = Math.Max(TaskConfig.OutputImageWidth / (double)TaskConfig.InputImageWidth, TaskConfig.OutputImageHeight / (double)TaskConfig.InputImageHeight);
                return (int)(Math.Log(scale) / Math.Log(2));
            }
        }
    }
}
