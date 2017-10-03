using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AutoWaifu.Lib
{
    static class TaskUtil
    {
        public static async Task WaitUntil(Func<bool> predicate)
        {
            while (!predicate())
                await Task.Delay(1).ConfigureAwait(false);
        }
    }
}
