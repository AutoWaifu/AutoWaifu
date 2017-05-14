﻿using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using WaifuLog;

namespace AutoWaifu.Lib.Waifu2x
{
    public abstract class IWaifuTask
    {
        public IWaifuTask(IResolutionResolver resolutionResolver,
                          WaifuConvertMode convertMode)
        {
            OutputResolutionResolver = resolutionResolver;
            ConvertMode = convertMode;
        }



        public abstract string InputFilePath { get; }
        



        public abstract IEnumerable<IWaifuTask> SubTasks { get; }
        public abstract int NumSubTasks { get; }
        public IResolutionResolver OutputResolutionResolver { get; set; }
        public WaifuConvertMode ConvertMode { get; set; }
        public ProcessPriorityClass ProcessPriority { get; set; } = ProcessPriorityClass.BelowNormal;




        public abstract bool IsRunning { get; }



        public event Action<IWaifuTask> TaskCompleted;
        public event Action<IWaifuTask, string> TaskFaulted;



        public async Task<bool> StartTask(string waifu2xCaffePath, string ffmpegPath)
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
                WaifuLogger.Exception(e);
                faulted = true;
            }

            bool taskSucceeded = false;
            if (!faulted)
            {
                try
                {
                    taskSucceeded = await Start(waifu2xCaffePath, ffmpegPath);
                }
                catch (Exception e)
                {
                    taskSucceeded = false;
                    TaskFaulted?.Invoke(this, $"Error occurred while processing: {e.Message}");
                    WaifuLogger.Exception(e);
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
                WaifuLogger.Exception(e);
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
                WaifuLogger.Exception(e);
                faulted = true;
            }

            try
            {
                return Dispose() && !faulted;
            }
            catch (Exception e)
            {
                TaskFaulted?.Invoke(this, $"Error occurred while running cleanup for a canceled task: {e.Message}");
                WaifuLogger.Exception(e);
                return false;
            }
        }


        protected abstract Task<bool> Start(string waifu2xCaffePath, string ffmpegPath);
        protected abstract Task<bool> Cancel();



        protected abstract bool Initialize();
        protected abstract bool Dispose();
    }
}
