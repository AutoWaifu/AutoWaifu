using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AutoWaifu.Lib.Jobs
{
    class JobPool
    {
        ConcurrentQueue<IJob> allJobs = new ConcurrentQueue<IJob>();
        public ReadOnlyCollection<IJob> AllJobs => new ReadOnlyCollection<IJob>(this.allJobs.ToList());

        public IEnumerable<IJob> RunningJobs { get; set; }
        public IEnumerable<IJob> PendingJobs { get; set; }

        public void Start()
        {
            throw new NotImplementedException();
        }

        public void WaitForRemaining()
        {
            throw new NotImplementedException();
        }
    }
}
