using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;

namespace AutoWaifu.Lib.Jobs
{
    public interface IJob
    {
        Task Run();
        Task Terminate();

        Task Suspend();
        Task Resume();

        string ResourceGroup { get; set; }

        ResourceConsumptionLevel ResourceConsumption { get; }

        JobState State { get; }

        int Priority { get; set; }

        event Action<IJob> Exited;
        event Action<IJob, JobState, JobState> StateChanged;
    }
}
