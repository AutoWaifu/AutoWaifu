using AutoWaifu.Lib.Waifu2x;
using PropertyChanged;
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

    [AddINotifyPropertyChangedInterface]
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

        WaifuTask runningTask = null;
        public WaifuTask RunningTask
        {
            get => runningTask;
            set
            {
                if (this.runningTask != null)
                    this.runningTask.TaskStateChanged -= RunningTask_TaskStateChanged;

                this.runningTask = value;

                if (this.runningTask != null)
                    this.runningTask.TaskStateChanged += RunningTask_TaskStateChanged;
                else
                    PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(TaskState)));
            }
        }

        private void RunningTask_TaskStateChanged(string obj)
        {
            RootConfig.AppDispatcher.Invoke(() =>
            {
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(TaskState)));

                StateChanged?.Invoke(this, State, State);
            });
        }

        public string FileExt { get { return Path.GetExtension(RelativeFilePath).ToLower(); } }

        public string TaskState
        {
            get
            {
                string state = RelativeFilePath;
                if (RunningTask?.TaskState != null && RunningTask.TaskState.Length != 0)
                    state = $"{state} - {RunningTask.TaskState}";

                return state;
            }
        }

        public WaifuImageType InputImageType
        {
            get
            {
                switch (FileExt)
                {
                    case ".png":
                        return WaifuImageType.Png;

                    case ".jpeg":
                        return WaifuImageType.Jpeg;

                    case ".jpg":
                        goto case ".jpeg";

                    case ".mp4":
                        return WaifuImageType.Mp4;

                    case ".gif":
                        return WaifuImageType.Gif;

                    case ".webm":
                        return WaifuImageType.Webm;

                    default:
                        return WaifuImageType.Invalid;
                }
            }
        }






        public WaifuImageType OutputImageType
        {
            get
            {
                switch (InputImageType)
                {
                    case WaifuImageType.Png: goto case WaifuImageType.Image;
                    case WaifuImageType.Jpeg: goto case WaifuImageType.Image;

                    case WaifuImageType.Image:
                        return InputImageType;

                    case WaifuImageType.Gif:
                        {
                            switch (AppSettings.Main.GifOutputType)
                            {
                                case AppSettings.AnimationOutputMode.GIF:
                                    return WaifuImageType.Gif;
                                case AppSettings.AnimationOutputMode.MP4:
                                    return WaifuImageType.Mp4;

                                default:
                                    throw new NotImplementedException();
                            }
                        }

                    case WaifuImageType.Mp4: goto case WaifuImageType.Video;
                    case WaifuImageType.Webm: goto case WaifuImageType.Video;

                    case WaifuImageType.Video:
                        return WaifuImageType.Mp4;

                    default:
                        return WaifuImageType.Invalid;
                }
            }
        }



        

        public string InputPath => Path.Combine(AppSettings.Main.InputDir.Trim('/', '\\'), RelativeFilePath.Trim('/', '\\'));

        public string OutputPath
        {
            get
            {
                string outputFile;
                string ext;
                switch (OutputImageType)
                {
                    default:
                        throw new InvalidOperationException();

                    case WaifuImageType.Jpeg:
                        goto case WaifuImageType.Png;

                    case WaifuImageType.Png:
                        ext = Path.GetExtension(RelativeFilePath);
                        break;

                    case WaifuImageType.Gif:
                        if (AppSettings.Main.GifOutputType == AppSettings.AnimationOutputMode.GIF)
                            ext = ".gif";
                        else
                            ext = ".mp4";
                        break;

                    case WaifuImageType.Mp4:
                        if (AppSettings.Main.VideoOutputType == AppSettings.AnimationOutputMode.MP4)
                            ext = ".mp4";
                        else
                            ext = ".gif";
                        break;

                    case WaifuImageType.Webm:
                        ext = ".webm";
                        break;
                }

                outputFile = Path.Combine(Path.GetDirectoryName(RelativeFilePath), Path.GetFileNameWithoutExtension(RelativeFilePath)) + ext;

                return Path.Combine(AppSettings.Main.OutputDir.Trim('/', '\\'), outputFile.Trim('/', '\\'));
            }
        }
    }
}
