using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AutoWaifu2
{
    static class StopwatchExt
    {
        public static TimeSpan Profile(Action action)
        {
            Stopwatch sw = new Stopwatch();
            sw.Start();
            action();
            sw.Stop();

            return TimeSpan.FromMilliseconds(sw.ElapsedMilliseconds);
        }

        public static async Task<TimeSpan> Profile(Func<Task> task)
        {
            Stopwatch sw = new Stopwatch();
            sw.Start();
            await task().ConfigureAwait(false);
            sw.Stop();

            return TimeSpan.FromMilliseconds(sw.ElapsedMilliseconds);
        }
    }
}
