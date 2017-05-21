using AutoWaifu.Lib.Waifu2x;
using System;
using System.Collections.Generic;
using System.ComponentModel;
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

    public class TaskItem : INotifyPropertyChanged
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
        public event PropertyChangedEventHandler PropertyChanged;

        public string RelativeFilePath { get; set; }

        IWaifuTask runningTask = null;
        public IWaifuTask RunningTask
        {
            get => runningTask;
            set
            {
                if (this.runningTask != null)
                    this.runningTask.TaskStateChanged -= RunningTask_TaskStateChanged;

                this.runningTask = value;

                if (this.runningTask != null)
                    this.runningTask.TaskStateChanged += RunningTask_TaskStateChanged;
            }
        }

        private void RunningTask_TaskStateChanged(string obj)
        {
            RootConfig.AppDispatcher.Invoke(() =>
            {
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(TaskState)));
            });
        }

        public string FileExt { get { return Path.GetExtension(RelativeFilePath); } }

        public string TaskState
        {
            get
            {
                string state = RelativeFilePath;
                if (RunningTask != null && RunningTask.TaskState?.Length != 0)
                    state += $" ({RunningTask.TaskState})";

                return state;
            }
        }



        

        public string InputPath => Path.Combine(AppSettings.Main.InputDir.Trim('/', '\\'), RelativeFilePath.Trim('/', '\\'));

        public string OutputPath
        {
            get
            {
                string outputFile;
                switch (FileExt.Trim('.'))
                {
                    case "jpeg":
                        goto case "png";

                    case "jpg":
                        goto case "png";

                    case "png":
                        outputFile = RelativeFilePath;
                        break;

                    case "gif":
                        outputFile = Path.GetFileNameWithoutExtension(RelativeFilePath) + ".mp4";
                        break;

                    default:
                        throw new InvalidOperationException();
                }

                return Path.Combine(AppSettings.Main.OutputDir.Trim('/', '\\'), outputFile.Trim('/', '\\'));
            }
        }
    }
}
