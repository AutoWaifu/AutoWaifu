using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AutoWaifu.Lib.Prediction
{
    public class PendingTask
    {
        public DateTime? StartTime { get; set; }
        public DateTime? EndTime { get; set; }

        public CompletedTaskMetric ToCompletedTaskMetric()
        {
            return new CompletedTaskMetric
            {
                
            };
        }
    }
}
