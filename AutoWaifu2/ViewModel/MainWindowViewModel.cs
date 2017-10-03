using AutoWaifu.Lib;
using AutoWaifu.Lib.Cui.Ffmpeg;
using AutoWaifu.Lib.FileSystem;
using AutoWaifu.Lib.Jobs;
using AutoWaifu.Lib.Waifu2x;
using AutoWaifu.Lib.Waifu2x.Tasks;
using Newtonsoft.Json;
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
    public class MainWindowViewModel : INotifyPropertyChanged
    {
        TaskQueue convertTaskQueue;
        JobQueue jobQueue;
        JobProcessor jobProcessor;
        FolderEnumeration inputFolderEnumeration, outputFolderEnumeration;

        ILogger Logger = Log.ForContext<MainWindowViewModel>();

        const string SupportedFilesFilter = ".jpg|.jpeg|.png|.mp4|.gif|.webm|.avi|.mov|.wmv";


        public TaskItemCollection TaskItems { get; private set; }

        public event PropertyChangedEventHandler PropertyChanged;

        void InvokePropertyChanged([CallerMemberName] string propertyName = null)
        {
            RootConfig.AppDispatcher.BeginInvoke(new Action(() =>
            {
                this.PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
            }));
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
            //ExtractEmbeddedUnmanagedAssemblies();

            Logger.Debug("Looking for {@SettingsFile} as the settings data", RootConfig.SettingsFilePath);

            if (!File.Exists(RootConfig.SettingsFilePath) || RootConfig.ForceNewConfig)
            {
                Logger.Debug("Could not find settings at {SettingsFile}, making new settings with defaults", RootConfig.SettingsFilePath);
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


            DispatcherTimer updateTimeRemainingTimer = new DispatcherTimer();
            updateTimeRemainingTimer.Interval = TimeSpan.FromSeconds(1);
            updateTimeRemainingTimer.Tick += (o, e) => InvokePropertyChanged(nameof(TimeRemaining));
            updateTimeRemainingTimer.Start();
        }

        private void WarnPathRename(string obj)
        {
            Logger.Warning("A file or folder {Path} was renamed, this behavior is not accounted for and may cause instability", obj);
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

            if (!Directory.Exists(Settings.InputDir))
            {
                inputFolderEnumeration = null;
                Logger.Warning("Could not find input file directory {InputDir}", Settings.InputDir);
                return;
            }

            Logger.Verbose("Built-in supported extensions: {SupportedExtensions}", SupportedFilesFilter);
            Logger.Verbose("User-defined ignored extensions: {IgnoredExtensions}", Settings.IgnoredFilesFilter);

            string[] supportedFiles = SupportedFilesFilter.Split('|');
            string[] ignoredFiles = Settings.IgnoredFilesFilter.Split('|');
            ignoredFiles = ignoredFiles.Select(ext => ext.ToLower().Trim()).ToArray();

            string[] enabledFiles = (from ext in supportedFiles
                                     where !ignoredFiles.Any(ign => ign.ToLower().Trim('.') == ext.ToLower().Trim('.'))
                                     select ext.StartsWith(".") ? ext : "." + ext).ToArray();

            string enabledFilesFilter = string.Join("|", enabledFiles);

            Logger.Verbose("Using search filter {SearchFilter}", enabledFilesFilter);

            inputFolderEnumeration = new FolderEnumeration(AppSettings.Main.InputDir);
            inputFolderEnumeration.Filter = enabledFilesFilter;

            inputFolderEnumeration.FileAdded += InputFolderEnumeration_FileAdded;
            inputFolderEnumeration.FileRemoved += InputFolderEnumeration_FileRemoved;
            inputFolderEnumeration.FolderAdded += InputFolderEnumeration_FolderAdded;
            inputFolderEnumeration.FileRenamed += WarnPathRename;
            inputFolderEnumeration.FolderRenamed += WarnPathRename;

            int numFiles = inputFolderEnumeration.FilePaths.Count();

            Logger.Verbose("Finished initializing input directory, found {NumFiles} files", numFiles);
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

            if (!Directory.Exists(Settings.OutputDir))
            {
                outputFolderEnumeration = null;
                Logger.Warning("Could not find output file directory {OutputDir}", Settings.OutputDir);
                return;
            }

            outputFolderEnumeration = new FolderEnumeration(AppSettings.Main.OutputDir);
            outputFolderEnumeration.Filter = ".jpg|.jpeg|.png|.mp4|.gif|.webm|.avi|.mov|.wmv";

            outputFolderEnumeration.FileRemoved += OutputFolderEnumeration_FileRemoved;
            outputFolderEnumeration.FolderRemoved += OutputFolderEnumeration_FolderRemoved;
            outputFolderEnumeration.FileRenamed += WarnPathRename;
            outputFolderEnumeration.FolderRenamed += WarnPathRename;

            int numFiles = outputFolderEnumeration.FilePaths.Count();

            Logger.Verbose("Finished initializing output directory, found {NumFiles} files", numFiles);
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
                return this.jobQueue.RunningJobs.Count();
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

                Logger.Information("Assigned AppSettings:\n{AppSettingsJson}", JsonConvert.SerializeObject(value, Formatting.Indented));

                _settings = value;
                if (convertTaskQueue != null && value != null)
                {
                    convertTaskQueue.QueueLength = value.MaxParallel;
                }

                if (reinitialize)
                {
                    Initialize(RootConfig.AppDispatcher);
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

            TaskItems = new TaskItemCollection();

            TaskItems.AddedInputItem += (item) => PendingInputFiles.Add(item);
            TaskItems.AddedItemProcessing += (item) => ProcessingQueueFiles.Add(item);
            TaskItems.AddedOutputItem += (item) => CompletedOutputFiles.Add(item);

            TaskItems.RemovedInputItem += (item) => PendingInputFiles.Remove(item);
            TaskItems.RemovedItemProcessing += (item) => ProcessingQueueFiles.Remove(item);
            TaskItems.RemovedOutputItem += (item) => CompletedOutputFiles.Remove(item);

            if (!Directory.Exists(Settings.InputDir))
            {
                try
                {
                    Directory.CreateDirectory(Settings.InputDir);
                }
                catch (Exception e)
                {
                    Logger.Error("Couldn't create the input directory at {InputDir}: {Exception}", Settings.InputDir, e);
                }
            }

            if (!Directory.Exists(Settings.OutputDir))
            {
                try
                {
                    Directory.CreateDirectory(Settings.OutputDir);
                }
                catch (Exception e)
                {
                    Logger.Error(e, "Couldn't create the output directory at {OutputDir}: {Exception}", Settings.OutputDir);
                }
            }

            InitInputEnumeration();
            InitOutputEnumeration();

            PendingInputFiles = new ThreadObservableCollection<TaskItem>(dispatcher);
            ProcessingQueueFiles = new ThreadObservableCollection<TaskItem>(dispatcher);
            CompletedOutputFiles = new ThreadObservableCollection<TaskItem>(dispatcher);

            convertTaskQueue = new TaskQueue();
            convertTaskQueue.QueueLength = AppSettings.Main.MaxParallel;

            this.jobQueue = new JobQueue();
            this.jobProcessor = new JobProcessor(this.jobQueue);

            PendingInputFiles.CollectionChanged += (o, e) => InvokePropertyChanged(nameof(PendingFileListLabel));
            ProcessingQueueFiles.CollectionChanged += (o, e) => InvokePropertyChanged(nameof(ProcessingFileListLabel));
            CompletedOutputFiles.CollectionChanged += (o, e) => InvokePropertyChanged(nameof(OutputFileListLabel));


            if (inputFolderEnumeration == null || outputFolderEnumeration == null)
            {
                Logger.Error("Couldn't open one of the input or output file folders (or possibly both) - will not load image tasks");
                return;
            }

            Logger.Debug("Finished initializing input/output folders");
            Logger.Debug("Generating diffs");


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

            int numCompletedTasks = TaskItems.Count(ti => ti.State == TaskItemState.Done);
            int numPendingTasks = TaskItems.Count(ti => ti.State == TaskItemState.Pending);

            Logger.Debug("Done, found {NumCompleted} completed files and {NumPending} pending files", numCompletedTasks, numPendingTasks);

            Logger.Debug("Cleaning temp folders");

            CleanTempFolders();

            Logger.Debug("Regenerating temp folders");

            Logger.Debug("Regenerating temp dir");
            Directory.CreateDirectory(Settings.TempDir);

            Logger.Debug("Regenerating temp output dir");
            Directory.CreateDirectory(Settings.TempDirInput);

            Logger.Debug("Regenerating temp input dir");
            Directory.CreateDirectory(Settings.TempDirOutput);
        }




        public void CleanTempFolders()
        {
            if (Directory.Exists(Settings.TempDirInput))
                Directory.Delete(Settings.TempDirInput, true);

            if (Directory.Exists(Settings.TempDirOutput))
                CleanTempOutputFolder();

            if (Directory.Exists(Settings.TempDir) &&
                !Directory.EnumerateFiles(Settings.TempDir, "*", SearchOption.AllDirectories).Any())
            {
                Directory.Delete(Settings.TempDir, true);
            }
        }

        public void CleanTempOutputFolder()
        {
            if (!Directory.Exists(Settings.TempDirOutput))
            {
                Logger.Warning("Could not find temp output directory {TempDirOutput}, will not clean", Settings.TempDirOutput);
                return;
            }

            var completedTasks = TaskItems[TaskItemState.Done].ToArray();
            

            var pendingTaskFileNames = TaskItems[TaskItemState.Pending].Select(ti => Path.GetFileNameWithoutExtension(ti.RelativeFilePath)).ToArray();

            var completedTaskFileNames = (from task in completedTasks
                                          select Path.GetFileNameWithoutExtension(task.RelativeFilePath)).ToArray();

            Logger.Verbose("Finding files in temp output dir");
            var outputTempFolderFiles = Directory.EnumerateFiles(Settings.TempDirOutput, "*", SearchOption.AllDirectories).ToArray();

            int numDiscarded = 0;
            Logger.Verbose("Checking {NumFiles} files for old frames to keep", outputTempFolderFiles.Length);


            foreach (var file in outputTempFolderFiles)
            {
                string fileName = Path.GetFileName(file);
                if (!pendingTaskFileNames.Any(f => fileName.StartsWith(f + "_")))
                {
                    File.Delete(file);
                    ++numDiscarded;
                }
            }

            Logger.Verbose("Deleted {NumDiscarded} temp files from previous sessions", numDiscarded);
            
        }





        public bool IsProcessing => processingTask != null;
        public bool IsStopped => !IsProcessing;



        Task processingTask = null;
        bool continueProcessing = false;

        public void StartProcessing()
        {
            Logger.Verbose("Called StartProcessing");

            if (processingTask != null)
                return;

            //if (!File.Exists(Path.Combine(Settings.Waifu2xCaffeDir, "waifu2x-caffe-cui.exe")))
            if (!File.Exists(Path.Combine(Settings.Waifu2xCaffeDir, "waifu2x-converter_x64.exe")))
            {
                Logger.Fatal("Couldn't find waifu2x-caffe-cui.exe in {@WaifuCaffeDir}", AppSettings.Main.Waifu2xCaffeDir);
                return;
            }

            if (!File.Exists(Path.Combine(Settings.FfmpegDir, "ffmpeg.exe")))
            {
                Logger.Fatal("Couldn't find ffmpeg.exe in {@FfmpegDir}", AppSettings.Main.FfmpegDir);
                return;
            }

            if (!Directory.Exists(Settings.InputDir))
            {
                Logger.Error("Will not start processing - couldn't find the input file directory {InputDir}", Settings.InputDir);
            }

            if (!Directory.Exists(Settings.OutputDir))
            {
                Logger.Error("Will not start processing - couldn't find the output file directory {OutputDir}", Settings.OutputDir);
            }

            _lastTaskCompleteTime = new DateTime();

            continueProcessing = true;
            processingTask = Task.Run(async () =>
            {
                while (continueProcessing)
                {
                    try
                    {
                        foreach (var task in this.convertTaskQueue.RunningTasks)
                            this.jobQueue.Enqueue(task.PollPendingJobs());

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

                            WaifuTask nextTask = null;
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

                                nextTask = animTask;
                            }
                            else
                            {
                                Logger.Warning("Unknown file type {FileType}", inputType);
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
                        Logger.Error("An unhandled top-level exception occurred while queuing tasks: " + e.ToString());
                        await Task.Delay(1000);
                    }

                    //  Let animation tasks start their subtasks so scheduling can be more accurate
                    await Task.Delay(100);
                }
            });


            this.jobProcessor.Start();


            this.InvokePropertyChanged(nameof(IsProcessing));
            this.InvokePropertyChanged(nameof(IsStopped));
        }

        public async Task StopProcessing()
        {
            Logger.Verbose("Canceling processing for currently running tasks");

            try
            {
                await this.jobProcessor.Stop(false);

                continueProcessing = false;

                Logger.Verbose("Canceling {@TaskCount} tasks", convertTaskQueue.RunningTasks.Count);

                int numRemaining = convertTaskQueue.RunningTasks.Count;

                foreach (var task in convertTaskQueue.RunningTasks)
                {
                    Task.Run(async () =>
                    {
                        Logger.Verbose("Canceling task of type {@TaskType} for file {@InputFilePath}", task.GetType().Name, task.InputFilePath);

                        var elapsedTime = await StopwatchExt.Profile(async () => await task.CancelTask().ConfigureAwait(false));

                        Logger.Verbose("Canceled task, took {@CancelTimeMs}ms", elapsedTime.TotalMilliseconds);

                        Interlocked.Decrement(ref numRemaining);
                    });
                }

                while (numRemaining > 0)
                    await Task.Delay(1).ConfigureAwait(false);

                if (processingTask != null)
                {
                    Logger.Verbose("Waiting for Task Queueing task to complete");
                    
                    var elapsedTime = await StopwatchExt.Profile(async () => await processingTask.ConfigureAwait(false)).ConfigureAwait(false);

                    Logger.Verbose("Task Queueing task exited, took {@CancelTimeMs}ms", elapsedTime.TotalMilliseconds);
                }

                Logger.Verbose("Successfully stopped processing for running tasks");

                processingTask = null;
            }
            catch (Exception e)
            {
                Logger.Error(e, "An error occurred while trying to stop processing tasks");
            }

            this.InvokePropertyChanged(nameof(IsProcessing));
            this.InvokePropertyChanged(nameof(IsStopped));
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

            await processingTask.ConfigureAwait(false);
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
