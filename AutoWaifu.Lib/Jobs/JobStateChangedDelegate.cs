using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AutoWaifu.Lib.Jobs
{
    public delegate void JobStateChangedDelegate(IJob job, JobState oldState, JobState newState);
}
