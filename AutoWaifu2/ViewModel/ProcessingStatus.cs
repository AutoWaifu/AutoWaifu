using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AutoWaifu2
{
    class ProcessingStatus
    {
        public int NumComplete { get; set; }

        //  Non-working images
        public int NumPending { get; set; }
        public int NumProcessing { get; set; }

        //  Working set
        public int NumImagesProcessing { get; set; }
        public int NumImagesProcessingPending { get; set; }


        public string ProcessingQueueStates { get; set; }


        public string QueueStatus { get; set; } = "Stopped";


        //  Implicit data from app environment
        public List<JToken> ParsedJsonLog { get; set; }
        public int LogSize { get; set; }
        public DateTime StartedAt { get; set; } = DateTime.Now;
        public DateTime? RefreshedAt { get; set; } = null;



        public bool IsHeadless => RootConfig.IsHeadless;


        public void RefreshImplicits()
        {
            string logJson = FileExt.ReadAllText(RootConfig.LogJsonFileLocation);

            logJson = "[" + logJson.Replace('\n', ',') + "]";

            var loadedLog = JsonConvert.DeserializeObject(logJson) as JArray;
            
            List<JToken> filteredLog = new List<JToken>(loadedLog.Count);

            foreach (var log in loadedLog.Reverse())
            {
                string callerMethod = log["Properties"]?["CallerMethodName"]?.ToString();
                string level = log["Level"].ToString();
                if (callerMethod != null && !callerMethod.Contains("uhttpsharp") && level != "Verbose")
                    filteredLog.Add(log);

                if (filteredLog.Count >= 1000)
                    break;
            }

            ParsedJsonLog = filteredLog;
            LogSize = ParsedJsonLog.Count;

            RefreshedAt = DateTime.Now;
        }
    }
}
