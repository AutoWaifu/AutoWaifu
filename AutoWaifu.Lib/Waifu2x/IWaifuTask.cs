using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AutoWaifu.Lib.Waifu2x
{
    public abstract class IWaifuTask : Loggable
    {
        public IWaifuTask(IResolutionResolver resolutionResolver,
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


        public abstract IEnumerable<IWaifuTask> SubTasks { get; }
        public abstract int NumSubTasks { get; }
        public IResolutionResolver OutputResolutionResolver { get; set; }
        public WaifuConvertMode ConvertMode { get; set; }
        public ProcessPriorityClass ProcessPriority { get; set; } = ProcessPriorityClass.BelowNormal;

        public bool WasCanceled { get; private set; } = false;




        public abstract bool IsRunning { get; }



        public event Action<IWaifuTask> TaskCompleted;
        public event Action<IWaifuTask, string> TaskFaulted;

        public async Task<bool> StartTask(string tempInputFolderPath, string tempOutputFolderPath, string waifu2xCaffePath, string ffmpegPath)
        {
            bool faulted = false;

            try
            {
                if (!Initialize())
                    return false;
            }
            catch (Exception e)
            {
                TaskFaulted?.Invoke(this, $"Error occurred during initialization: {e.Message}");
                Logger.Error("An exception occurred during initialization: {@Exception}", e);
                faulted = true;
            }

            bool taskSucceeded = false;
            if (!faulted)
            {
                try
                {
                    taskSucceeded = await Start(tempInputFolderPath, tempOutputFolderPath, waifu2xCaffePath, ffmpegPath);
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
                if (!faulted)
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
                if (!(await Cancel()))
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
