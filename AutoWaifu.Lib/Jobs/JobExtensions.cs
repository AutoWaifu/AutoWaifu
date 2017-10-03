using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using static System.Runtime.CompilerServices.ConfiguredTaskAwaitable;

namespace AutoWaifu.Lib.Jobs
{
    public static class JobExtensions
    {
        public static bool IsExecuting(this IJob job)
        {
            return job.State == JobState.Running ||
                   job.State == JobState.Suspended;
        }

        public static bool IsInQueue(this IJob job)
        {
            return job.State == JobState.Pending ||
                   job.State == JobState.Running ||
                   job.State == JobState.Suspended;
        }

        public static async Task WaitForExit(this IEnumerable<IJob> jobs)
        {
            while (jobs.Any(j => j.State == JobState.Running ||
                                 j.State == JobState.Suspended))
                await Task.Delay(1).ConfigureAwait(false);
        }

        public static ConfiguredTaskAwaiter GetAwaiter(this IJob job)
        {
            return Task.Run(async () =>
            {
                while (job.IsInQueue())
                    await Task.Delay(1).ConfigureAwait(false);

            }).ConfigureAwait(false).GetAwaiter();
        }
    }
}
