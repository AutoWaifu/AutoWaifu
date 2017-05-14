using AutoWaifu.Lib.Waifu2x;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AutoWaifu2
{
    public enum TaskItemState
    {
        Unknown,
        Pending,
        Processing,
        Done,

        Faulted
    }

    public class TaskItem
    {
        TaskItemState state = TaskItemState.Unknown;
        public TaskItemState State
        {
            get { return this.state; }
            set
            {
                if (this.state != value)
                {
                    this.StateChanged?.Invoke(this, this.state, value);
                    this.state = value;
                }
            }
        }


        public delegate void TaskItemStateChangedHandler(TaskItem item, TaskItemState oldState, TaskItemState newState);



        public event TaskItemStateChangedHandler StateChanged;


        public string RelativeFilePath { get; set; }
        public IWaifuTask RunningTask { get; set; }



        public string FileExt { get { return Path.GetExtension(RelativeFilePath); } }



        

        public string InputPath => Path.Combine(AppSettings.Main.InputDir.Trim('/', '\\'), RelativeFilePath.Trim('/', '\\'));

        public string OutputPath => Path.Combine(AppSettings.Main.OutputDir.Trim('/', '\\'), RelativeFilePath.Trim('/', '\\'));
    }
}
