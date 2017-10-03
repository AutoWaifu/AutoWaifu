using AutoWaifu.Lib.Jobs;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AutoWaifu.Lib.Waifu2x
{
    public abstract class WaifuTask : Loggable
    {
        public WaifuTask(IResolutionResolver resolutionResolver,
                          WaifuConvertMode convertMode)
        {
            OutputResolutionResolver = resolutionResolver;
            ConvertMode = convertMode;
        }



        public abstract string InputFilePath { get; }
        

        public abstract string TaskState { get; }
        public event Action<string> TaskStateChanged;

        protected void InvokeTaskStateChanged()
        {
            TaskStateChanged?.Invoke(TaskState);
        }
        


        public IResolutionResolver OutputResolutionResolver { get; set; }
        public WaifuConvertMode ConvertMode { get; set; }
        public ProcessPriorityClass ProcessPriority { get; set; } = ProcessPriorityClass.BelowNormal;

        public bool WasCanceled { get; private set; } = false;
        public bool WasFaulted { get; private set; } = false;



        ConcurrentQueue<IJob> jobQueue = new ConcurrentQueue<IJob>();

        protected void QueueJob(IJob job)
        {
            jobQueue.Enqueue(job);
        }

        protected void QueueJobs(IEnumerable<IJob> jobs)
        {
            foreach (var job in jobs)
                jobQueue.Enqueue(job);
        }

        /// <summary>
        /// Gets the next job for this task and removes it from the task's job queue.
        /// </summary>
        public IEnumerable<IJob> PollPendingJobs()
        {
            IJob job;

            while (this.jobQueue.TryDequeue(out job))
                yield return job;
        }




        public abstract bool IsRunning { get; }



        public event Action<WaifuTask> TaskCompleted;
        public event Action<WaifuTask, string> TaskFaulted;

        public async Task<bool> StartTask(string tempInputFolderPath, string tempOutputFolderPath, string waifu2xCaffePath, string ffmpegPath)
        {
            WasFaulted = false;

            try
            {
                if (!Initialize())
                    return false;
            }
            catch (Exception e)
            {
                TaskFaulted?.Invoke(this, $"Error occurred during initialization: {e.Message}");
                Logger.Error("An exception occurred during initialization: {@Exception}", e);
                WasFaulted = true;
            }

            bool taskSucceeded = false;
            if (!WasFaulted)
            {
                try
                {
                    taskSucceeded = await Start(tempInputFolderPath, tempOutputFolderPath, waifu2xCaffePath, ffmpegPath).ConfigureAwait(false);
                }
                catch (Exception e)
                {
                    taskSucceeded = false;
                    TaskFaulted?.Invoke(this, $"Error occurred while processing: {e.Message}");
                    Logger.Error("An exception occurred while processing: {@Exception}", e);
                }
            }

            TaskCompleted?.Invoke(this);

            try
            {
                return Dispose();
            }
            catch (Exception e)
            {
                if (!WasFaulted)
                    TaskFaulted?.Invoke(this, $"Error occurred while running cleanup: {e.Message}");

                return false;
            }
        }

        public async Task<bool> CancelTask()
        {
            WasCanceled = true;

            try
            {
                if (!IsRunning)
                {
                    return Dispose();
                }
            }
            catch (Exception e)
            {
                TaskFaulted?.Invoke(this, $"Error occurred while running cleanup: {e.Message}");
                Logger.Error(e, "Error occurred while running cleanup");
                return false;
            }

            bool faulted = false;

            try
            {
                if (!(await Cancel().ConfigureAwait(false)))
                    return false;
            }
            catch (Exception e)
            {
                TaskFaulted?.Invoke(this, $"Error occurred while canceling a task: {e.Message}");
                Logger.Error(e, "Error occurred while canceling a task");
                faulted = true;
            }

            try
            {
                return Dispose() && !faulted;
            }
            catch (Exception e)
            {
                TaskFaulted?.Invoke(this, $"Error occurred while running cleanup for a canceled task: {e.Message}");
                Logger.Error(e, "Error occurred while running cleanup for a canceled task");
                return false;
            }
        }


        protected abstract Task<bool> Start(string tempInputFolderPath, string tempOutputFolderPath, string waifu2xCaffePath, string ffmpegPath);
        protected abstract Task<bool> Cancel();



        protected abstract bool Initialize();
        protected abstract bool Dispose();
    }
}
