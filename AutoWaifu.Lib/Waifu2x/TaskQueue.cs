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
        List<IWaifuTask> runningTasks = new List<IWaifuTask>();

        T RunLockedTaskOp<T>(Func<T> op)
        {

                return op();
        }

        public string DefaultTempPath { get; set; } = "./tmp";
        public string DefaultTempInputFolderName { get; set; } = "input";
        public string DefaultTempOutputFolderName { get; set; } = "output";


        public string DefaultTempInputPath => Path.Combine(DefaultTempPath, DefaultTempInputFolderName);
        public string DefaultTempOutputPath => Path.Combine(DefaultTempPath, DefaultTempOutputFolderName);

        public List<IWaifuTask> RunningTasks => new List<IWaifuTask>(RunLockedTaskOp(() => this.runningTasks));

        public int QueueLength { get; set; }

        public bool CanQueueTask => RunLockedTaskOp(() => runningTasks.Count < QueueLength);

        public bool TryQueueTask<T>(T taskToQueue) where T : IWaifuTask
        {
            return RunLockedTaskOp(() =>
            {
                if (this.runningTasks.Count >= QueueLength)
                    return false;

                this.runningTasks.Add(taskToQueue);

                return true;
            });
        }

        public bool TryCompleteTask<T>(T queuedTask) where T : IWaifuTask
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
