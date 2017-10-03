using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AutoWaifu.Lib.Jobs
{
    public enum JobState
    {
        Pending,
        Running,
        Terminated,
        Suspended,
        Completed,
        Faulted
    }
}
