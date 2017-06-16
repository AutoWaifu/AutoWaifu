using AutoWaifu.Lib;
using AutoWaifu.Lib.Cui.Ffmpeg;
using AutoWaifu.Lib.FileSystem;
using AutoWaifu.Lib.Waifu2x;
using AutoWaifu.Lib.Waifu2x.Tasks;
using PropertyChanged;
using Serilog;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Threading;

namespace AutoWaifu2
{
    [AddINotifyPropertyChangedInterface]
    public class MainWindowViewModel : INotifyPropertyChanged
    {
        TaskQueue convertTaskQueue;
        FolderEnumeration inputFolderEnumeration, outputFolderEnumeration;

        ILogger Logger = Log.ForContext<MainWindowViewModel>();


        public TaskItemCollection TaskItems { get; private set; } = new TaskItemCollection();

        public event PropertyChangedEventHandler PropertyChanged;

        void InvokePropertyChanged([CallerMemberName] string propertyName = null)
        {
            this.PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        void ExtractEmbeddedUnmanagedAssemblies()
        {
            Logger.Debug("Loading embedded DLLs");

            var assembly = Assembly.GetExecutingAssembly();
            var fileNames = new[]
            {
                "DirectShowLib-2005.dll",
                "EVRPresenter64.dll"
            };

            foreach (var fileName in fileNames)
            {
                if (File.Exists(fileName))
                    continue;


                var resourceName = $"AutoWaifu2.{fileName}";
                var resourcePath = Path.GetFullPath(fileName);

                Logger.Verbose("Extracting {DllName}", fileName);

                using (Stream stream = assembly.GetManifestResourceStream(resourceName))
                {
                    byte[] bytes = new byte[stream.Length];
                    stream.Read(bytes, 0, (int)stream.Length);

                    File.WriteAllBytes(resourcePath, bytes);
                }
            }
        }


        public MainWindowViewModel()
        {
            ExtractEmbeddedUnmanagedAssemblies();

            Logger.Debug("Loading {@SettingsFile} as the settings data", RootConfig.SettingsFilePath);

            if (!File.Exists(RootConfig.SettingsFilePath) || RootConfig.ForceNewConfig)
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

                    Logger.Warning(e, "Loading settings.json failed, resetting to defaults!");
                }
            }

            AppSettings.SetMainSettings(Settings);





            TaskItems.AddedInputItem += (item) => PendingInputFiles.Add(item);
            TaskItems.AddedItemProcessing += (item) => ProcessingQueueFiles.Add(item);
            TaskItems.AddedOutputItem += (item) => CompletedOutputFiles.Add(item);
            TaskItems.RemovedInputItem += (item) => PendingInputFiles.Remove(item);
            TaskItems.RemovedItemProcessing += (item) => ProcessingQueueFiles.Remove(item);
            TaskItems.RemovedOutputItem += (item) => CompletedOutputFiles.Remove(item);


            DispatcherTimer updateTimeRemainingTimer = new DispatcherTimer();
            updateTimeRemainingTimer.Interval = TimeSpan.FromSeconds(1);
            updateTimeRemainingTimer.Tick += (o, e) => InvokePropertyChanged(nameof(TimeRemaining));
            updateTimeRemainingTimer.Start();
        }

        private void WarnPathRename(string obj)
        {
            Logger.Warning("A file or folder '{@Path}' was renamed, this behavior is not accounted for and may cause instability", obj);
            MessageBox.Show("A file or folder was renamed; this might interfere with AutoWaifu operations! Please close AutoWaifu and complete your renaming, and restart it when you are done.");
        }

        private void InitInputEnumeration()
        {
            Logger.Verbose("Initializing Input directory enumeration");

            if (inputFolderEnumeration != null)
            {
                inputFolderEnumeration.FileAdded -= InputFolderEnumeration_FileAdded;
                inputFolderEnumeration.FolderAdded -= InputFolderEnumeration_FolderAdded;
                inputFolderEnumeration.FileRenamed -= WarnPathRename;
                inputFolderEnumeration.FolderRenamed -= WarnPathRename;
            }

            inputFolderEnumeration = new FolderEnumeration(AppSettings.Main.InputDir);
            inputFolderEnumeration.Filter = ".jpg|.jpeg|.png|.mp4|.gif";

            inputFolderEnumeration.FileAdded += InputFolderEnumeration_FileAdded;
            inputFolderEnumeration.FileRemoved += InputFolderEnumeration_FileRemoved;
            inputFolderEnumeration.FolderAdded += InputFolderEnumeration_FolderAdded;
            inputFolderEnumeration.FileRenamed += WarnPathRename;
            inputFolderEnumeration.FolderRenamed += WarnPathRename;
        }

        

        private void InitOutputEnumeration()
        {
            Logger.Verbose("Initializing Output directory enumeration");

            if (outputFolderEnumeration != null)
            {
                outputFolderEnumeration.FileRemoved -= OutputFolderEnumeration_FileRemoved;
                outputFolderEnumeration.FolderRemoved -= OutputFolderEnumeration_FolderRemoved;
                outputFolderEnumeration.FileRenamed -= WarnPathRename;
                outputFolderEnumeration.FolderRenamed -= WarnPathRename;
            }

            outputFolderEnumeration = new FolderEnumeration(AppSettings.Main.OutputDir);
            outputFolderEnumeration.Filter = ".jpg|.jpeg|.png|.gif|.mp4";

            outputFolderEnumeration.FileRemoved += OutputFolderEnumeration_FileRemoved;
            outputFolderEnumeration.FolderRemoved += OutputFolderEnumeration_FolderRemoved;
            outputFolderEnumeration.FileRenamed += WarnPathRename;
            outputFolderEnumeration.FolderRenamed += WarnPathRename;
        }





        #region ViewModel Properties

        public ThreadObservableCollection<TaskItem> PendingInputFiles { get; private set; }
        public ThreadObservableCollection<TaskItem> ProcessingQueueFiles { get; private set; }
        public ThreadObservableCollection<TaskItem> CompletedOutputFiles { get; private set; }

        public string PendingFileListLabel => PendingInputFiles.Count == 0 ? "Pending Files" : $"Pending Files ({PendingInputFiles.Count})";
        public string ProcessingFileListLabel => ProcessingQueueFiles.Count == 0 ? "Processing Queue" : $"Processing Queue ({ProcessingQueueFiles.Count})";
        public string OutputFileListLabel => CompletedOutputFiles.Count == 0 ? "Upscaled Files" : $"Upscaled Files ({CompletedOutputFiles.Count})";
        
        public ObservableCollection<TaskItem> AllFiles { get; private set; }





        public int NumRunningImageTasks
        {
            get
            {
                if (convertTaskQueue.RunningTasks.Count == 0)
                    return 0;

                int numTasks = 0;
                var tasks = convertTaskQueue.RunningTasks.ToList();
                foreach (var task in tasks)
                {
                    var asAnimation = task as AnimationTask;

                    if (asAnimation == null)
                    {
                        numTasks++;
                    }
                    else
                    {
                        var numSubTasks = asAnimation.NumSubTasks;
                        if (numSubTasks > 0)
                            numTasks += numSubTasks;
                        else
                            numTasks++;
                    }
                }

                return numTasks;
            }
        }

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
                {
                    convertTaskQueue.QueueLength = value.MaxParallel;

                    foreach (var animTask in convertTaskQueue.RunningTasks.Where(t => t is AnimationTask).Cast<AnimationTask>())
                    {
                        animTask.MaxWaifuTaskThreads = (int)Math.Round(Settings.MaxParallel * Settings.AnimationParallelizationMaxThreadsFactor);
                        animTask.ParallelizeWaifuTasks = Settings.ParallelizeAnimationConversion;
                    }
                }

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

                if (numRemaining == 0 || _convertTimeHistory.Count == 0 || !IsProcessing)
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
            Logger.Verbose("Initializing window ViewModel - Load Input/Output file diffs");

            if (!Directory.Exists(Settings.InputDir))
                Directory.CreateDirectory(Settings.InputDir);

            if (!Directory.Exists(Settings.OutputDir))
                Directory.CreateDirectory(Settings.OutputDir);

            InitInputEnumeration();
            InitOutputEnumeration();

            PendingInputFiles = new ThreadObservableCollection<TaskItem>(dispatcher);
            ProcessingQueueFiles = new ThreadObservableCollection<TaskItem>(dispatcher);
            CompletedOutputFiles = new ThreadObservableCollection<TaskItem>(dispatcher);

            convertTaskQueue = new TaskQueue();
            convertTaskQueue.QueueLength = AppSettings.Main.MaxParallel;

            PendingInputFiles.CollectionChanged += (o, e) => InvokePropertyChanged(nameof(PendingFileListLabel));
            ProcessingQueueFiles.CollectionChanged += (o, e) => InvokePropertyChanged(nameof(ProcessingFileListLabel));
            CompletedOutputFiles.CollectionChanged += (o, e) => InvokePropertyChanged(nameof(OutputFileListLabel));




            //  Run initial input/output diff for missing folders, and determine which files have already been processed
            var outputDiff = inputFolderEnumeration.DiffAgainst(outputFolderEnumeration);
            var missingFoldersInOutput = outputDiff.Where(diff => diff.Type == FolderDifference.DifferenceType.FolderMissing).ToArray();
            foreach (var missing in missingFoldersInOutput)
                Directory.CreateDirectory(missing.AbsolutePath);

            


            var missingFilesInOutput = outputDiff.Where(diff => diff.Type == FolderDifference.DifferenceType.FileMissing);
            var filesToProcess = missingFilesInOutput.Select(f => AppRelativePath.CreateInput(f.AbsolutePath));
            var preProcessedFiles = outputFolderEnumeration.FilePaths;

            var inputFiles = inputFolderEnumeration.RelativeFilePaths.ToArray();
            foreach (var input in inputFiles)
            {
                var newTaskItem = new TaskItem { RelativeFilePath = input, State = TaskItemState.Unknown };

                if (newTaskItem.State == TaskItemState.Unknown)
                {
                    if (File.Exists(Path.Combine(outputFolderEnumeration.FolderPath, newTaskItem.OutputPath)))
                        newTaskItem.State = TaskItemState.Done;
                    else
                        newTaskItem.State = TaskItemState.Pending;
                }

                TaskItems.Add(newTaskItem);
            }

            CleanTempFolders();

            Directory.CreateDirectory(Settings.TempDir);
            Directory.CreateDirectory(Settings.TempDirInput);
            Directory.CreateDirectory(Settings.TempDirOutput);
        }




        public void CleanTempFolders()
        {
            if (Directory.Exists(Settings.TempDirInput))
                Directory.Delete(Settings.TempDirInput, true);

            if (Directory.Exists(Settings.TempDirOutput))
                CleanTempOutputFolder();

            if (Directory.Exists(Settings.TempDir) &&
                Directory.EnumerateFiles(Settings.TempDir, "*", SearchOption.AllDirectories).Count() == 0)
            {
                Directory.Delete(Settings.TempDir, true);
            }
        }

        public void CleanTempOutputFolder()
        {
            var completedTasks = TaskItems[TaskItemState.Done];
            var completedTaskFileNames = from task in completedTasks
                                         select Path.GetFileNameWithoutExtension(task.RelativeFilePath);

            var outputTempFolderFiles = Directory.EnumerateFiles(Settings.TempDirOutput, "*", SearchOption.AllDirectories);

            var oldUpscaledFrameFiles = from file in outputTempFolderFiles
                                        where completedTaskFileNames.Any(f => Path.GetFileName(file).StartsWith(f + "_"))
                                        select file;

            foreach (var file in outputTempFolderFiles)
            {
                var matchingCompletedTasks = completedTaskFileNames.Where(f => Path.GetFileName(file).StartsWith(f + "_")).ToList();

            }

            //this PART IS BROKEN

            foreach (var oldframe in oldUpscaledFrameFiles)
                File.Delete(oldframe);
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
                Logger.Fatal("Couldn't find waifu2x-caffe-cui.exe in {@WaifuCaffeDir}", AppSettings.Main.Waifu2xCaffeDir);
                return;
            }

            if (!File.Exists(Path.Combine(AppSettings.Main.FfmpegDir, "ffmpeg.exe")))
            {
                Logger.Fatal("Couldn't find ffmpeg.exe in {@FfmpegDir}", AppSettings.Main.FfmpegDir);
                return;
            }

            _lastTaskCompleteTime = new DateTime();

            continueProcessing = true;
            processingTask = Task.Run(async () =>
            {
                while (continueProcessing)
                {
                    try
                    {
                        var inputItems = TaskItems[TaskItemState.Pending];
                        //  TODO - Figure out how to schedule standard image tasks in conjunction with those that are animation sub-tasks
                        //if (convertTaskQueue.CanQueueTask && inputItems.Length > 0)
                        if (NumRunningImageTasks < Settings.MaxParallel && inputItems.Length > 0)
                        {
                            var nextTaskItem = TaskItems[TaskItemState.Pending].First();

                            IResolutionResolver imageResolutionResolver;
                            switch (Settings.ResolutionMode)
                            {
                                case AppSettings.ResolutionResolverMode.MaxSize:
                                    imageResolutionResolver = Settings.MaxSizeResolution;
                                    break;

                                case AppSettings.ResolutionResolverMode.ScaleFactor:
                                    imageResolutionResolver = Settings.ScaleResolution;
                                    break;

                                case AppSettings.ResolutionResolverMode.TargetMegapixels:
                                    imageResolutionResolver = Settings.MegapixelResolution;
                                    break;

                                default:
                                    Logger.Fatal("Invalid image resolution mode \"{@ResolutionMode}\"! All processing will STOP!", Settings.ResolutionMode);
                                    return;
                            }

                            IResolutionResolver animationResolutionResolver = imageResolutionResolver;

                            string ffmpegPath = Path.Combine(Settings.FfmpegDir, "ffmpeg.exe");

                            IWaifuTask nextTask = null;
                            var inputType = nextTaskItem.InputImageType;
                            if (inputType.HasFlag(WaifuImageType.Image))
                            {
                                nextTask = new ImageTask(nextTaskItem.InputPath, nextTaskItem.OutputPath, imageResolutionResolver, Settings.ConversionMode);
                            }
                            else if (nextTaskItem.InputImageType.HasFlag(WaifuImageType.Animated))
                            {
                                IAnimationExtractor extractor;
                                IAnimationTaskCompileProcess compileProcess = null;
                                if (inputType.HasFlag(WaifuImageType.Video))
                                {
                                    extractor = new AnimationExtractorVideo(ffmpegPath);

                                    switch (Settings.VideoOutputType)
                                    {
                                        case AppSettings.AnimationOutputMode.GIF:
                                            compileProcess = new AnimationTaskCompileProcessGif(ffmpegPath);
                                            break;
                                        case AppSettings.AnimationOutputMode.MP4:
                                            IFfmpegOptions ffmpegOptions = new FfmpegCompatibilityOptions
                                            {
                                                TargetCompatibility = FfmpegCompatibilityOptions.OutputCompatibilityType.GoodQualityMediumCompatibility
                                            };

                                            compileProcess = new AnimationTaskCompileProcessVideo(ffmpegPath, ffmpegOptions);
                                            break;
                                    }
                                }
                                else // Must be a gif
                                {
                                    if (Settings.GifFrameExtractionMode == AppSettings.GifAnimationExtractionMode.ImageMagick)
                                        extractor = new AnimationExtractorGifImageMagick { DespeckleAmount = Settings.GifDenoiseAmount };
                                    else
                                        extractor = new AnimationExtractorGifFfmpeg(ffmpegPath) { DenoiseAmount = Settings.GifDenoiseAmount };

                                    switch (Settings.GifOutputType)
                                    {
                                        case AppSettings.AnimationOutputMode.GIF:
                                            compileProcess = new AnimationTaskCompileProcessGif(ffmpegPath);
                                            break;

                                        case AppSettings.AnimationOutputMode.MP4:
                                            IFfmpegOptions ffmpegOptions = new FfmpegCompatibilityOptions
                                            {
                                                TargetCompatibility = FfmpegCompatibilityOptions.OutputCompatibilityType.GoodQualityMediumCompatibility
                                            };

                                            compileProcess = new AnimationTaskCompileProcessVideo(ffmpegPath, ffmpegOptions);
                                            break;
                                    }
                                }

                                var animTask = new AnimationTask(nextTaskItem.InputPath, nextTaskItem.OutputPath, compileProcess, extractor, animationResolutionResolver, Settings.ConversionMode);
                                animTask.MaxWaifuTaskThreads = (int)Math.Round(Settings.MaxParallel * Settings.AnimationParallelizationMaxThreadsFactor);
                                animTask.ParallelizeWaifuTasks = Settings.ParallelizeAnimationConversion;

                                nextTask = animTask;
                            }
                            else
                            {
                                nextTaskItem.State = TaskItemState.Faulted;
                                continue;
                            }

                            if (convertTaskQueue.TryQueueTask(nextTask))
                            {
                                nextTaskItem.State = TaskItemState.Processing;
                                nextTaskItem.RunningTask = nextTask;

                                DateTime taskStartTime = DateTime.Now;

                                Logger.Debug("Starting task for {@FilePath}", nextTaskItem.RelativeFilePath);

                                nextTask.TaskCompleted += (task) =>
                                {
                                    convertTaskQueue.TryCompleteTask(nextTask);
                                    nextTaskItem.RunningTask = null;

                                    if (!task.WasCanceled)
                                        nextTaskItem.State = TaskItemState.Done;
                                    else
                                        nextTaskItem.State = TaskItemState.Pending;

                                    DateTime taskEndTime = DateTime.Now;
                                    _convertTimeHistory.Add(taskEndTime - taskStartTime);
                                    if (_convertTimeHistory.Count > _maxConvertTimeHistoryCount)
                                        _convertTimeHistory.RemoveAt(0);

                                    _lastTaskCompleteTime = taskEndTime;
                                };

                                nextTask.TaskFaulted += (task, msg) =>
                                {
                                    Logger.Error("The task for {InputPath} failed to complete with the following message: {FaultMessage}", task.InputFilePath, msg);
                                };

                                nextTask.StartTask(Settings.TempDirInput, Settings.TempDirOutput, Settings.Waifu2xCaffeDir, Path.Combine(Settings.FfmpegDir, "ffmpeg.exe"));
                            }
                        }
                    }
                    catch (Exception e)
                    {
                        Logger.Error(e, "An unhandled top-level exception occurred while queuing tasks");
                        await Task.Delay(1000);
                    }

                    //  Let animation tasks start their subtasks so scheduling can be more accurate
                    await Task.Delay(1000);
                }
            });
        }

        public async Task StopProcessing()
        {
            Logger.Verbose("Canceling processing for currently running tasks");

            try
            {
                continueProcessing = false;

                Logger.Verbose("Canceling {@TaskCount} tasks", convertTaskQueue.RunningTasks.Count);

                int numRemaining = convertTaskQueue.RunningTasks.Count;

                foreach (var task in convertTaskQueue.RunningTasks)
                {
                    Task.Run(async () =>
                    {
                        Logger.Verbose("Canceling task of type {@TaskType} for file {@InputFilePath}", task.GetType().Name, task.InputFilePath);

                        var elapsedTime = await StopwatchExt.Profile(async () => await task.CancelTask());

                        Logger.Verbose("Canceled task, took {@CancelTimeMs}ms", elapsedTime.TotalMilliseconds);

                        Interlocked.Decrement(ref numRemaining);
                    });
                }

                while (numRemaining > 0)
                    await Task.Delay(1);

                if (processingTask != null)
                {
                    Logger.Verbose("Waiting for Task Queueing task to complete");
                    
                    var elapsedTime = await StopwatchExt.Profile(async () => await processingTask);

                    Logger.Verbose("Task Queueing task exited, took {@CancelTimeMs}ms", elapsedTime.TotalMilliseconds);
                }

                Logger.Verbose("Successfully stopped processing for running tasks");

                processingTask = null;
            }
            catch (Exception e)
            {
                Logger.Error(e, "An error occurred while trying to stop processing tasks");
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

            RootConfig.AppDispatcher.Invoke(() =>
            {
                TaskItems.Add(new TaskItem
                {
                    RelativeFilePath = AppRelativePath.Create(filePath),
                    State = TaskItemState.Pending
                });
            });
        }

        private void InputFolderEnumeration_FileRemoved(string obj)
        {
            RootConfig.AppDispatcher.Invoke(() =>
            {
                TaskItems.Remove(obj);
            });
        }


        #endregion

    }
}
