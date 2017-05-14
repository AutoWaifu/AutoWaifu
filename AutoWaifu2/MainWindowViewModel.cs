using AutoWaifu.Lib;
using AutoWaifu.Lib.FileSystem;
using AutoWaifu.Lib.Waifu2x;
using PropertyChanged;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Threading;
using WaifuLog;

namespace AutoWaifu2
{
    [ImplementPropertyChanged]
    public class MainWindowViewModel : INotifyPropertyChanged
    {
        TaskQueue convertTaskQueue;
        FolderEnumeration inputFolderEnumeration, outputFolderEnumeration;


        public TaskItemCollection TaskItems { get; private set; } = new TaskItemCollection();

        IResolutionResolver OutputResolutionResolver
        {
            get
            {
                switch (Settings.ResolutionMode)
                {
                    case AppSettings.ResolutionResolverMode.MaxSize: return Settings.MaxSizeResolution;
                    case AppSettings.ResolutionResolverMode.ScaleFactor: return Settings.ScaleResolution;
                    case AppSettings.ResolutionResolverMode.TargetMegapixels: return Settings.MegapixelResolution;

                    default:
                        throw new InvalidOperationException();
                }
            }
        }

        public event PropertyChangedEventHandler PropertyChanged;

        void InvokePropertyChanged([CallerMemberName] string propertyName = null)
        {
            this.PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }


        public MainWindowViewModel()
        {
            if (!File.Exists(RootConfig.SettingsFilePath) || true)
            {
                Settings = new AppSettings();
                Settings.SaveToFile(RootConfig.SettingsFilePath);
            }
            else
            {
                try
                {
                    Settings = AppSettings.LoadFromFile(RootConfig.SettingsFilePath);
                }
                catch (Exception e)
                {
                    Settings = new AppSettings();
                    Settings.SaveToFile(RootConfig.SettingsFilePath);

                    WaifuLogger.ConfigWarning($"Loading settings.json failed, resetting to defaults! Info: {e.Message}");
                }
            }

            AppSettings.SetMainSettings(Settings);

            try
            {
                if (!Directory.Exists(AppSettings.Main.InputDir))
                    Directory.CreateDirectory(AppSettings.Main.InputDir);
                if (!Directory.Exists(AppSettings.Main.OutputDir))
                    Directory.CreateDirectory(AppSettings.Main.OutputDir);

                if (!Directory.Exists(AppSettings.Main.TempDirInput))
                    Directory.CreateDirectory(AppSettings.Main.TempDirInput);
                if (!Directory.Exists(AppSettings.Main.TempDirOutput))
                    Directory.CreateDirectory(AppSettings.Main.TempDirOutput);
            }
            catch (Exception e)
            {
                WaifuLogger.ConfigWarning($"There was a problem setting up the input/output folders, is your settings.json correct? Info: {e.Message}");
            }


            TaskItems.AddedInputItem += (item) => PendingInputFiles.Add(item.RelativeFilePath);
            TaskItems.AddedItemProcessing += (item) => ProcessingQueueFiles.Add(item.RelativeFilePath);
            TaskItems.AddedOutputItem += (item) => CompletedOutputFiles.Add(item.RelativeFilePath);
            TaskItems.RemovedInputItem += (item) => PendingInputFiles.Remove(item.RelativeFilePath);
            TaskItems.RemovedItemProcessing += (item) => ProcessingQueueFiles.Remove(item.RelativeFilePath);
            TaskItems.RemovedOutputItem += (item) => CompletedOutputFiles.Remove(item.RelativeFilePath);


            DispatcherTimer updateTimeRemainingTimer = new DispatcherTimer();
            updateTimeRemainingTimer.Interval = TimeSpan.FromSeconds(1);
            updateTimeRemainingTimer.Tick += (o, e) => InvokePropertyChanged(nameof(TimeRemaining));
            updateTimeRemainingTimer.Start();
        }

        private void WarnPathRename(string obj)
        {
            MessageBox.Show("A file or folder was renamed; this might interfere with AutoWaifu operations! Please close AutoWaifu and complete your renaming, and restart it when you are done.");
        }

        private void InitInputEnumeration()
        {
            if (inputFolderEnumeration != null)
            {
                inputFolderEnumeration.FileAdded -= InputFolderEnumeration_FileAdded;
                inputFolderEnumeration.FolderAdded -= InputFolderEnumeration_FolderAdded;
                inputFolderEnumeration.FileRenamed -= WarnPathRename;
                inputFolderEnumeration.FolderRenamed -= WarnPathRename;
            }

            inputFolderEnumeration = new FolderEnumeration(AppSettings.Main.InputDir);
            inputFolderEnumeration.Filter = ".jpg|.png|.jpeg";

            inputFolderEnumeration.FileAdded += InputFolderEnumeration_FileAdded;
            inputFolderEnumeration.FolderAdded += InputFolderEnumeration_FolderAdded;
            inputFolderEnumeration.FileRenamed += WarnPathRename;
            inputFolderEnumeration.FolderRenamed += WarnPathRename;
        }

        private void InitOutputEnumeration()
        {
            if (outputFolderEnumeration != null)
            {
                outputFolderEnumeration.FileRemoved -= OutputFolderEnumeration_FileRemoved;
                outputFolderEnumeration.FolderRemoved -= OutputFolderEnumeration_FolderRemoved;
                outputFolderEnumeration.FileRenamed -= WarnPathRename;
                outputFolderEnumeration.FolderRenamed -= WarnPathRename;
            }

            outputFolderEnumeration = new FolderEnumeration(AppSettings.Main.OutputDir);

            outputFolderEnumeration.FileRemoved += OutputFolderEnumeration_FileRemoved;
            outputFolderEnumeration.FolderRemoved += OutputFolderEnumeration_FolderRemoved;
            outputFolderEnumeration.FileRenamed += WarnPathRename;
            outputFolderEnumeration.FolderRenamed += WarnPathRename;
        }





        #region ViewModel Properties

        public ThreadObservableCollection<string> PendingInputFiles { get; private set; }
        public ThreadObservableCollection<string> ProcessingQueueFiles { get; private set; }
        public ThreadObservableCollection<string> CompletedOutputFiles { get; private set; }

        AppSettings _settings;
        public AppSettings Settings
        {
            get => _settings;
            set
            {
                bool reinitialize = false;
                if (_settings != null && (_settings.InputDir != value.InputDir || _settings.OutputDir != value.OutputDir))
                    reinitialize = true;

                _settings = value;
                if (convertTaskQueue != null && value != null)
                    convertTaskQueue.QueueLength = value.MaxParallel;

                if (reinitialize)
                {
                    Initialize(this.PendingInputFiles.Dispatcher);

                }
            }
        }


        int _maxConvertTimeHistoryCount = 4;
        List<TimeSpan> _convertTimeHistory = new List<TimeSpan>();
        DateTime _lastTaskCompleteTime = new DateTime();



        public string TimeRemaining
        {
            get
            {
                int numRemaining = PendingInputFiles.Count + ProcessingQueueFiles.Count;

                if (numRemaining == 0 || _convertTimeHistory.Count == 0)
                    return null;

                double avgTimeSeconds = _convertTimeHistory.Sum(ts => ts.TotalSeconds);
                double remainingTimeSeconds = avgTimeSeconds * numRemaining / Settings.MaxParallel;

                remainingTimeSeconds -= (DateTime.Now - _lastTaskCompleteTime).TotalSeconds;
                TimeSpan timeSpan = TimeSpan.FromSeconds(remainingTimeSeconds);

                Dictionary<string, int> remainingTime = new Dictionary<string, int>
                {
                    { "day", (int)timeSpan.TotalDays },
                    { "hour", timeSpan.Hours },
                    { "minute", timeSpan.Minutes },
                    { "second", Math.Max(1, timeSpan.Seconds) }
                };

                var asList = remainingTime.ToList();
                for (int i = 0; i < asList.Count; i++)
                {
                    if (asList[i].Value > 0)
                    {
                        string result = String.Empty;

                        var majorInterval = asList[i];
                        result += String.Format("{1} {0}", majorInterval.Key, majorInterval.Value);
                        if (asList[i].Value > 1)
                            result += 's';

                        if (i != asList.Count - 1)
                        {
                            var minorInterval = asList[i + 1];
                            result += String.Format(" {1} {0}", minorInterval.Key, minorInterval.Value);
                            if (asList[i].Value > 1)
                                result += 's';
                        }

                        if (result == "1 second")
                            result += "...";

                        return result;
                    }
                }

                //  Should never happen
                throw new Exception();
            }
        }

        #endregion





        #region Public API

        public void Initialize(Dispatcher dispatcher)
        {
            InitInputEnumeration();
            InitOutputEnumeration();

            PendingInputFiles = new ThreadObservableCollection<string>(dispatcher);
            ProcessingQueueFiles = new ThreadObservableCollection<string>(dispatcher);
            CompletedOutputFiles = new ThreadObservableCollection<string>(dispatcher);

            convertTaskQueue = new TaskQueue();
            convertTaskQueue.QueueLength = AppSettings.Main.MaxParallel;


            



            //  Run initial input/output diff for missing folders, and determine which files have already been processed
            var outputDiff = inputFolderEnumeration.DiffAgainst(outputFolderEnumeration);
            var missingFoldersInOutput = outputDiff.Where(diff => diff.Type == FolderDifference.DifferenceType.FolderMissing).ToArray();
            foreach (var missing in missingFoldersInOutput)
                Directory.CreateDirectory(missing.AbsolutePath);

            


            var missingFilesInOutput = outputDiff.Where(diff => diff.Type == FolderDifference.DifferenceType.FileMissing);
            var filesToProcess = missingFilesInOutput.Select(f => AppRelativePath.CreateInput(f.AbsolutePath));
            var preProcessedFiles = outputFolderEnumeration.FilePaths;


            foreach (var completed in preProcessedFiles)
            {
                TaskItems.Add(new TaskItem
                {
                    RelativeFilePath = AppRelativePath.Create(completed),
                    RunningTask = null,
                    State = TaskItemState.Done
                });
            }

            foreach (var input in filesToProcess)
            {
                TaskItems.Add(new TaskItem
                {
                    RelativeFilePath = AppRelativePath.Create(input),
                    State = TaskItemState.Pending
                });
            }
        }


        public bool IsProcessing => processingTask != null;




        Task processingTask = null;
        bool continueProcessing = false;

        public void StartProcessing()
        {
            if (processingTask != null)
                return;

            if (!File.Exists(Path.Combine(AppSettings.Main.Waifu2xCaffeDir, "waifu2x-caffe-cui.exe")))
            {
                WaifuLogger.ConfigWarning($"Couldn't find waifu2x-caffe-cui.exe in {AppSettings.Main.Waifu2xCaffeDir}");
                return;
            }

            if (!File.Exists(Path.Combine(AppSettings.Main.FfmpegDir, "ffmpeg.exe")))
            {
                WaifuLogger.ConfigWarning($"Couldn't find ffmpeg.exe in {AppSettings.Main.FfmpegDir}");
                return;
            }

            continueProcessing = true;
            processingTask = Task.Run(async () =>
            {
                while (continueProcessing)
                {
                    var inputItems = TaskItems[TaskItemState.Pending];
                    if (convertTaskQueue.CanQueueTask && inputItems.Length > 0)
                    {
                        var nextTaskItem = TaskItems[TaskItemState.Pending].First();
                        var builder = new WaifuTaskBuilder(OutputResolutionResolver, AppSettings.Main.ConversionMode);
                        IWaifuTask nextTask = builder.TaskFor(nextTaskItem.InputPath, nextTaskItem.OutputPath);

                        if (convertTaskQueue.TryQueueTask(nextTask))
                        {
                            nextTaskItem.State = TaskItemState.Processing;

                            DateTime taskStartTime = DateTime.Now;

                            nextTask.TaskCompleted += (task) =>
                            {
                                convertTaskQueue.TryCompleteTask(nextTask);
                                nextTaskItem.State = TaskItemState.Done;

                                DateTime taskEndTime = DateTime.Now;
                                _convertTimeHistory.Add(taskEndTime - taskStartTime);
                                if (_convertTimeHistory.Count > _maxConvertTimeHistoryCount)
                                    _convertTimeHistory.RemoveAt(0);

                                _lastTaskCompleteTime = taskEndTime;
                            };

                            nextTask.TaskFaulted += (task, msg) =>
                            {
                                WaifuLogger.LogicError($"The task for {task.InputFilePath} failed to complete with the following message: {msg}");
                            };
                            
                            nextTask.StartTask(AppSettings.Main.Waifu2xCaffeDir, AppSettings.Main.FfmpegDir);
                        }
                    }

                    await Task.Delay(10);
                }
            });
        }

        public async Task StopProcessing()
        {
            try
            {
                if (processingTask == null)
                    return;

                continueProcessing = false;

                foreach (var task in convertTaskQueue.RunningTasks)
                    await task.CancelTask();

                await processingTask.ConfigureAwait(true);
                await processingTask;
                processingTask = null;
            }
            catch (Exception e)
            {

            }
        }

        public async Task WaitForProcessingToFinish()
        {
            if (processingTask == null)
                return;

            continueProcessing = false;

            foreach (var task in convertTaskQueue.RunningTasks)
            {
                while (task.IsRunning)
                    await Task.Delay(1);
            }

            await processingTask;
            processingTask = null;
        }

        #endregion




        #region Events for GUI


        private void OutputFolderEnumeration_FolderRemoved(string folderPath)
        {
            if (TaskItems.Any(item => item.OutputPath.Contains(folderPath)))
                Directory.CreateDirectory(folderPath);
        }

        private void OutputFolderEnumeration_FileRemoved(string filePath)
        {
            var item = TaskItems[filePath];
            if (item != null)
                item.State = TaskItemState.Pending;
        }

        private void InputFolderEnumeration_FolderAdded(string folderPath)
        {
            string outputFolderPath = AppRelativePath.CreateOutput(folderPath);
            Directory.CreateDirectory(outputFolderPath);
        }

        private void InputFolderEnumeration_FileAdded(string filePath)
        {
            if (TaskItems.Any(ti => filePath == ti.InputPath))
                return;

            TaskItems.Add(new TaskItem
            {
                RelativeFilePath = AppRelativePath.Create(filePath),
                State = TaskItemState.Pending
            });
        }


        #endregion

    }
}
