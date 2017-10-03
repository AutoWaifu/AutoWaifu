 using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AutoWaifu.Lib.Jobs
{
    /// <summary>
    /// <para>
    /// A convenience wrapper for implementing IJob. Automatically invokes Exited and StateChanged events,
    /// and prevents invalid run/terminate/suspend/resume operations.
    /// </para>
    /// <para>
    /// Automatically assigned faulted state on exception.
    /// </para>
    /// <para>
    /// It is up to the implementor to assign <see cref="State"/> to Completed when done.
    /// </para>
    /// </summary>
    public abstract class Job : Loggable, IJob
    {
        public abstract ResourceConsumptionLevel ResourceConsumption { get; }

        public int Priority { get; set; }

        public event Action<IJob> Exited;
        public event Action<IJob, JobState, JobState> StateChanged;

        public virtual string ResourceGroup { get; set; } = "Unassigned (Job base)";

        protected bool SuspendRequested { get; private set; }
        protected bool TerminateRequested { get; private set; }

        /// <summary>
        /// False if the DoRun method has returned, otherwise true.
        /// </summary>
        protected bool IsRunExecuting { get; private set; }


        JobState currentState = JobState.Pending;
        public JobState State
        {
            get => this.currentState;
            set
            {
                if (this.currentState != value)
                {
                    JobState oldState = this.currentState;

                    this.currentState = value;
                    StateChanged?.Invoke(this, oldState, this.currentState);

                    if (this.currentState == JobState.Completed ||
                        this.currentState == JobState.Faulted ||
                        this.currentState == JobState.Terminated)
                    {
                        Exited?.Invoke(this);
                    }
                }
            }
        }



        #region Inherited Interface

        protected abstract Task DoRun();

        protected abstract Task DoTerminate();

        protected virtual Task DoResume()
        {
            return DoRun();
        }

        protected virtual Task DoSuspend()
        {
            return DoTerminate();
        }

        #endregion


        #region Public Interface

        public async Task Run()
        {
            if (this.State != JobState.Pending)
                return;

            try
            {
                IsRunExecuting = true;
                this.State = JobState.Running;
                await DoRun();
            }
            catch
            {
                State = JobState.Faulted;
            }
            finally
            {
                Logger.Verbose("{JobType} finished running", this.GetType().Name);
                IsRunExecuting = false;
            }
        }

        public async Task Terminate()
        {
            if (this.State != JobState.Running && this.State != JobState.Suspended)
                return;

            TerminateRequested = true;

            try
            {
                this.State = JobState.Terminated;
                await DoTerminate();
            }
            catch
            {
                this.State = JobState.Faulted;
            }

            TerminateRequested = false;
        }

        public async Task Suspend()
        {
            if (this.State != JobState.Running)
                return;

            SuspendRequested = true;

            try
            {
                this.State = JobState.Suspended;
                await DoSuspend();
            }
            catch
            {
                this.State = JobState.Faulted;
            }

            SuspendRequested = false;
        }

        public async Task Resume()
        {
            if (this.State != JobState.Suspended)
                return;

            try
            {
                State = JobState.Running;
                await DoResume();
            }
            catch
            {
                State = JobState.Faulted;
            }
        }

        #endregion
    }
}
