using Serilog;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AutoWaifu.Lib.Jobs
{
    public class JobProcessor
    {
        static ILogger Logger = Log.ForContext<JobProcessor>();
        
        Task lowResourcePoolTask;
        Task highResourcePoolTask;

        bool isSuspended = false;

        public JobProcessor(JobQueue jobQueue)
        {
            Queue = jobQueue;
        }

        public JobQueue Queue { get; }

        public JobProcessorOptions Options { get; set; } = new JobProcessorOptions();

        public bool IsRunning { get; private set; }

        public void Start()
        {
            IsRunning = true;
            lowResourcePoolTask = Task.Factory.StartNew(RunLowResourceJobs, TaskCreationOptions.LongRunning).Unwrap();
            highResourcePoolTask = Task.Factory.StartNew(RunHighResourceJobs, TaskCreationOptions.LongRunning).Unwrap();
        }

        public async Task Stop(bool waitForRemaining)
        {
            if (IsRunning)
                return;

            IsRunning = false;

            Task[] runningJobs;
            if (waitForRemaining)
                runningJobs = Queue.RunningJobs.Select(j => TaskUtil.WaitUntil(() => !j.IsExecuting())).ToArray();
            else
                runningJobs = Queue.RunningJobs.Select(j => j.Terminate()).ToArray();

            while (!Task.WaitAll(runningJobs, 1))
                await Task.Delay(50).ConfigureAwait(false);

            IsRunning = false;

            var poolTasks = new[] { lowResourcePoolTask, highResourcePoolTask };
            while (!Task.WaitAll(poolTasks, 1))
                await Task.Delay(50).ConfigureAwait(false);
        }

        async Task TerminateAll()
        {
            var runningJobs = Queue.RunningJobs.Select(j => j.Terminate()).ToArray();
            while (!Task.WaitAll(runningJobs, 1))
                await Task.Delay(50).ConfigureAwait(false);
        }


        public async Task Suspend()
        {
            this.isSuspended = true;

            var suspendedTasks = Queue.RunningJobs.Select(j => j.Suspend()).ToArray();

            while (!Task.WaitAll(suspendedTasks, 1))
                await Task.Delay(50).ConfigureAwait(false);
        }

        public async Task Resume()
        {
            this.isSuspended = false;

            var resumeTasks = (from job in Queue
                               where job.State == JobState.Suspended
                               select job.Resume()).ToArray();

            while (!Task.WaitAll(resumeTasks, 1))
                await Task.Delay(50).ConfigureAwait(false);
        }


        async Task RunLowResourceJobs()
        {
            while (IsRunning)
            {
                await Task.Delay(1).ConfigureAwait(false);

                if (this.isSuspended)
                    continue;

                var nextJob = Queue.Next(ResourceConsumptionLevel.Low, ResourceConsumptionLevel.Medium);
                nextJob?.Run();

                if (nextJob != null)
                    Logger.Debug("Running job: {JobType}", nextJob.GetType().Name);
            }
        }

        async Task RunHighResourceJobs()
        {
            while (IsRunning)
            {
                await Task.Delay(1).ConfigureAwait(false);

                if (this.isSuspended)
                    continue;

                if (Queue[ResourceConsumptionLevel.High].Where(j => j.State == JobState.Running).Count() > Options.HighResourceConsumptionPoolSize)
                    continue;

                var nextJob = Queue.Next(ResourceConsumptionLevel.High);
                nextJob?.Run();

                if (nextJob != null)
                    Logger.Debug("Running job: {JobType}", nextJob.GetType().Name);
            }
        }
    }
}
