using System;
using System.Collections;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AutoWaifu.Lib.Jobs
{
    public class JobQueue : IEnumerable<IJob>
    {
        ConcurrentDictionary<ResourceConsumptionLevel, ConcurrentSet<IJob>> mappedJobs;


        public IEnumerable<IJob> this[params ResourceConsumptionLevel[] resourceConsumption]
        {
            get
            {
                return this.mappedJobs.Where(m => resourceConsumption.Contains(m.Key) || resourceConsumption.Length == 0)
                                      .SelectMany(m => m.Value);
            }
        }

        public IEnumerable<IJob> RunningJobs => mappedJobs.SelectMany(m => m.Value)
                                                          .Where(j => j.State == JobState.Running || j.State == JobState.Suspended);

        /// <summary>
        /// Gets the next job of the given priority, by order.
        /// </summary>
        /// <param name="resourceConsumption"></param>
        /// <returns></returns>
        public IJob Next(params ResourceConsumptionLevel[] resourceConsumption)
        {
            return this[resourceConsumption].Where(j => j.State == JobState.Pending)
                                            .OrderBy(j => j.Priority)
                                            .FirstOrDefault();
        }



        public event Action<IJob> JobEnqueued;
        public event Action<IJob> JobDequeued;



        public JobQueue()
        {
            this.mappedJobs = new ConcurrentDictionary<ResourceConsumptionLevel, ConcurrentSet<IJob>>();
            Reset().Wait();
        }

        /// <summary>
        /// Waits for running jobs to finish and clears the queue
        /// </summary>
        /// <returns></returns>
        public async Task Reset()
        {
            await RunningJobs.WaitForExit();

            mappedJobs[ResourceConsumptionLevel.High] = new ConcurrentSet<IJob>();
            mappedJobs[ResourceConsumptionLevel.Medium] = new ConcurrentSet<IJob>();
            mappedJobs[ResourceConsumptionLevel.Low] = new ConcurrentSet<IJob>();
        }

        /// <summary>
        /// Returns a task that completes when the job has finished execution
        /// </summary>
        public Task Enqueue(IJob job)
        {
            if (job.ResourceConsumption == ResourceConsumptionLevel.Unknown)
            {
                throw new InvalidOperationException($"{nameof(JobQueue)} does not support jobs with Unknown resource consumption");
            }

            var resourceJobs = this.mappedJobs[job.ResourceConsumption];
            resourceJobs.Add(job);

            JobEnqueued?.Invoke(job);

            return new Task(async () =>
            {
                while (job.State != JobState.Completed &&
                       job.State != JobState.Faulted &&
                       job.State != JobState.Terminated)
                {
                    await Task.Delay(1).ConfigureAwait(false);
                }

                if (resourceJobs.Contains(job))
                    JobDequeued?.Invoke(job);

                resourceJobs.Remove(job);
            });
        }

        public Task Enqueue(IEnumerable<IJob> jobs)
        {
            foreach (var job in jobs)
                Enqueue(job);

            return jobs.WaitForExit();
        }

        public IEnumerator<IJob> GetEnumerator()
        {
            return this.mappedJobs.SelectMany(m => m.Value).GetEnumerator();
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }
    }
}
