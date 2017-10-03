using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AutoWaifu.Lib.Waifu2x
{
    public class TaskQueue
    {
        List<WaifuTask> runningTasks = new List<WaifuTask>();

        T RunLockedTaskOp<T>(Func<T> op)
        {
            return op();
        }

        public string DefaultTempPath { get; set; } = "./tmp";
        public string DefaultTempInputFolderName { get; set; } = "input";
        public string DefaultTempOutputFolderName { get; set; } = "output";


        public string DefaultTempInputPath => Path.Combine(DefaultTempPath, DefaultTempInputFolderName);
        public string DefaultTempOutputPath => Path.Combine(DefaultTempPath, DefaultTempOutputFolderName);

        public List<WaifuTask> RunningTasks => new List<WaifuTask>(RunLockedTaskOp(() => this.runningTasks));

        public int QueueLength { get; set; }

        public bool CanQueueTask => RunLockedTaskOp(() => runningTasks.Count < QueueLength);

        public bool TryQueueTask<T>(T taskToQueue) where T : WaifuTask
        {
            return RunLockedTaskOp(() =>
            {
                if (this.runningTasks.Count >= QueueLength)
                    return false;

                this.runningTasks.Add(taskToQueue);

                return true;
            });
        }

        public bool TryCompleteTask<T>(T queuedTask) where T : WaifuTask
        {
            return RunLockedTaskOp(() =>
            {
                if (this.runningTasks.Contains(queuedTask))
                {
                    this.runningTasks.Remove(queuedTask);
                    return true;
                }
                else
                {
                    return false;
                }
            });
        }
    }
}
