using AutoWaifu.Lib.Waifu2x;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Management;
using System.Runtime.Serialization.Formatters.Binary;
using System.Text;
using System.Threading.Tasks;

namespace AutoWaifu.Lib.Prediction
{
    [Serializable]
    public class TimePredictor
    {
        internal TimePredictor()
        {
            History = new List<TimePoint>();
        }


        static TimePredictor _instance;
        public static TimePredictor Main
        {
            get
            {
                if (_instance == null)
                    _instance = new TimePredictor();
                return _instance;
            }
        }

        public List<TimePoint> History;

        public void LoadFrom(string filePath)
        {
            if (!File.Exists(filePath))
                return;

            BinaryFormatter formatter = new BinaryFormatter();

            try
            {
                using (var file = File.OpenRead(filePath))
                {
                    var result = (TimePredictor)formatter.Deserialize(file);

                    this.History = result.History.Where(tp => tp != null).ToList();
                }
            }
            catch (Exception e)
            {
                File.Delete(filePath);
            }
        }

        public void SaveTo(string filePath)
        {
            BinaryFormatter formatter = new BinaryFormatter();
            using (var file = File.OpenWrite(filePath))
            {
                formatter.Serialize(file, this);
            }
        }





        public TimeSpan? ExpectedTaskDuration(IWaifuTask requestedTask)
        {
            throw new NotImplementedException();

            //double SizeDist(IWaifuTask task, TimePoint data)
            //{
            //    double inputDist = Math.Sqrt(Math.Pow(requestedTask.InputFileSize.Width - data.TaskConfig.InputImageWidth, 2) + Math.Pow(requestedTask.InputFileSize.Height - data.TaskConfig.InputImageHeight, 2));
            //    //double outputDist = Math.Sqrt(Math.Pow(requestedTask.OutputFileSize.Width - data.ConfigForTask.OutputImageWidth, 2) + Math.Pow(requestedTask.OutputFileSize.Height - data.ConfigForTask.OutputImageHeight, 2));
            //    //return inputDist + outputDist;

            //    return inputDist;
            //}
            

            //History.RemoveAll(tp => tp == null);

            //var orderedBestMatches = (from data in History
            //                         //orderby SizeDist(requestedTask, data) * (Math.Abs(TaskScaleFactor(requestedTask) - DataScaleFactor(data)) + 1)
            //                          orderby SizeDist(requestedTask, data)
            //                          select data).ToList();

            //if (orderedBestMatches.Count == 0)
            //    return null;

            //if (orderedBestMatches.Count == 1)
            //    return orderedBestMatches.First().ConvertDuration;

            //var firstBest = orderedBestMatches[0];
            //var secondBest = orderedBestMatches[1];

            //var firstSimilarity = SizeDist(requestedTask, firstBest);
            //var secondSimilarity = SizeDist(requestedTask, secondBest);

            ////  TODO - Extrapolate for image sizes larger than in history
            ////  Need to do some PDE for solving for best solution

            //firstSimilarity = Math.Max(1, firstSimilarity);
            //secondSimilarity = Math.Max(1, secondSimilarity);

            //var firstFactor = firstSimilarity / (firstSimilarity + secondSimilarity);
            //var secondFactor = secondSimilarity / (firstSimilarity + secondSimilarity);



            //double grad = (firstBest.NumGeneratedPixels - secondBest.NumGeneratedPixels) / Math.Sqrt(Math.Pow(firstBest.NumGeneratedPixels - secondBest.NumGeneratedPixels, 2) - Math.Pow(firstBest.ProcessingCycles - secondBest.ProcessingCycles, 2));
            //double expectedCycles = firstBest.ProcessingCycles + ((long)requestedTask.NumGeneratedPixels - firstBest.NumGeneratedPixels) * grad;

            ////if (DataScaleFactor(firstBest) < TaskScaleFactor(requestedTask))
            ////{
            ////expectedSeconds = firstBest.ConvertDuration.TotalSeconds * firstFactor + secondBest.ConvertDuration.TotalSeconds * secondFactor;
            ////}
            ////else
            ////{

            ////}

            //uint clockSpeed;
            //using (ManagementObject mo = new ManagementObject("Win32_Processor.DeviceID='CPU0'"))
            //    clockSpeed = (uint)mo["CurrentClockSpeed"];

            //double expectedSeconds = expectedCycles / clockSpeed;
            //expectedSeconds /= AppSettings.Main.MaxParallel;

            //return TimeSpan.FromSeconds(expectedSeconds);
        }
    }
}
