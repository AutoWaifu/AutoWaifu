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
        public string TextLog { get; set; }
        [JsonIgnore]
        public string TextLogHtml { get; set; }
        public JArray ParsedJsonLog { get; set; }

        public DateTime StartedAt { get; set; } = DateTime.Now;
        public DateTime? RefreshedAt { get; set; } = null;



        public bool IsHeadless => RootConfig.IsHeadless;


        public void RefreshImplicits()
        {
            string logJson = FileExt.ReadAllText(RootConfig.LogJsonFileLocation);

            logJson = "[" + logJson.Replace('\n', ',') + "]";

            var loadedLog = JsonConvert.DeserializeObject(logJson) as JArray;

            ParsedJsonLog = new JArray();

            foreach (var log in loadedLog)
            {
                string callerMethod = log["Properties"]?["CallerMethodName"]?.ToString();
                if (callerMethod != null && !callerMethod.Contains("uhttpsharp"))
                    ParsedJsonLog.Add(log);
            }

            List<string> logLines = new List<string>();

            foreach (var entry in ParsedJsonLog.Reverse())
            {
                string logText = entry["RenderedMessage"].ToString();
                logLines.Add(logText);
            }

            TextLog = string.Join("\n", logLines);

            TextLogHtml = string.Join("\n", from log in logLines
                                            select $"<p>{log.Replace("\n", "<br>")}</p>");

            string tabHtml = "<span style='display:inline-block;width:2em;'></span>";
            TextLogHtml = TextLogHtml.Replace("\t", tabHtml);
            TextLogHtml = TextLogHtml.Replace("  ", tabHtml);

            RefreshedAt = DateTime.Now;
        }
    }
}
