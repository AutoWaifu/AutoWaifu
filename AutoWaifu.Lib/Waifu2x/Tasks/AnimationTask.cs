using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace AutoWaifu.Lib.Waifu2x.Tasks
{
    public class AnimationTask : IWaifuTask
    {
        public AnimationTask(string inputFilePath, string outputFilePath, IAnimationTaskCompileProcess compileProcess, IAnimationExtractor animationFrameExtractor, IResolutionResolver resolutionResolver, WaifuConvertMode convertMode) : base(resolutionResolver, convertMode)
        {
            AnimationFrameExtractor = animationFrameExtractor;
            InputFilePath = inputFilePath;
            OutputFilePath = outputFilePath;
            CompileProcess = compileProcess;
        }

        bool _canceled = false;
        string _tempInputDir = null;

        public IAnimationExtractor AnimationFrameExtractor { get; }

        public IAnimationTaskCompileProcess CompileProcess { get; }

        public override string InputFilePath { get; }

        public string OutputFilePath { get; }

        string _taskState = null;
        public override string TaskState => this._taskState;

        IEnumerable<IWaifuTask> subTasks = null;
        public override IEnumerable<IWaifuTask> SubTasks => this.subTasks;

        int numSubTasks = 0;
        public override int NumSubTasks => numSubTasks;

        bool _isRunning = false;
        public override bool IsRunning => this._isRunning;

        public bool ParallelizeWaifuTasks { get; set; } = true;

        int _maxWaifuTaskThreads = 2;
        public int MaxWaifuTaskThreads
        {
            get
            {
                if (ParallelizeWaifuTasks)
                    return _maxWaifuTaskThreads;
                else
                    return 1;
            }
            set
            {
                _maxWaifuTaskThreads = value;
            }
        }

        void Cleanup()
        {
            var inputFrameFiles = from file in Directory.EnumerateFiles(_tempInputDir)
                                  where Path.GetFileNameWithoutExtension(file).StartsWith(Path.GetFileNameWithoutExtension(InputFilePath))
                                  select file;

            foreach (var inputFrame in inputFrameFiles)
            {
                File.Delete(inputFrame);
            }
        }

        protected override async Task<bool> Cancel()
        {
            this._canceled = true;

            while (this.IsRunning)
                await Task.Delay(1);

            Cleanup();

            return true;
        }

        protected override bool Dispose()
        {
            return true;
        }

        protected override bool Initialize()
        {
            return true;
        }

        protected override async Task<bool> Start(string tempInputFolderPath, string tempOutputFolderPath, string waifu2xCaffePath, string ffmpegPath)
        {
            _isRunning = true;
            _tempInputDir = tempInputFolderPath;

            this._taskState = "extracting animation frames";
            InvokeTaskStateChanged();

            var extractResult = await AnimationFrameExtractor.ExtractFrames(InputFilePath, tempInputFolderPath, () => this._canceled);

            if (this._canceled)
            {
                Logger.Information("Terminating frame extraction for {AnimationPath} since this task has been cancelled", this.InputFilePath);
                _isRunning = false;
                return false;
            }

            if (extractResult == null)
            {
                Logger.Error("An error occurred while extracting the animation frames for {InputFilePath} using {TaskExtractorTypeName}", this.InputFilePath, AnimationFrameExtractor.GetType().Name);
                _isRunning = false;
                return false;
            }

            TaskQueue tasks = new TaskQueue();

            int numCompleted = 0;
            int numStarted = 0;
            Queue<string> remainingImages = new Queue<string>(extractResult.ExtractedFiles);

            var outputResolutionResolver = this.OutputResolutionResolver;
            
            ImageResolution inputImageResolution = null;
            ImageResolution previousResultOutputImageResolution = null;
            ImageResolution outputImageResolution;
            bool canUseOldFrames = false;

            using (var firstFrame = Image.FromFile(extractResult.ExtractedFiles.First()))
            {
                inputImageResolution = new ImageResolution
                {
                    WidthInt = firstFrame.Width,
                    HeightInt = firstFrame.Height
                };

                outputImageResolution = outputResolutionResolver.Resolve(inputImageResolution);
                var resolvedResolutionSize = outputImageResolution.Width * outputImageResolution.Height / 1e6;
                var maxResolution = CompileProcess.MaxOutputResolutionMegapixels;
                if (resolvedResolutionSize > maxResolution) // Max output resolution is 8MP
                {
                    Logger.Warning("Output resolution for animation frame {InputAnimation} is too high for the {ImageCompiler} animation frame compiler at {OutputResolutionMegapixels} megapixels, limiting output size to {MaxCompileResolutionMegapixels} megapixels.", InputFilePath, CompileProcess.GetType().Name, resolvedResolutionSize, maxResolution);

                    outputResolutionResolver = new TargetPixelCountResolutionResolver(maxResolution * 1e6f);
                    outputImageResolution = outputResolutionResolver.Resolve(inputImageResolution);
                }
            }

            //  Take into account how output images are resized to have even dimensions after upscaling
            outputImageResolution.WidthInt += outputImageResolution.WidthInt % 2;
            outputImageResolution.HeightInt += outputImageResolution.HeightInt % 2;

            string firstPreviousResultOutputFramePath = Path.Combine(tempOutputFolderPath, Path.GetFileName(extractResult.ExtractedFiles.First()));
            if (File.Exists(firstPreviousResultOutputFramePath))
            {
                previousResultOutputImageResolution = ImageHelper.GetImageResolution(firstPreviousResultOutputFramePath);
            }

            if (previousResultOutputImageResolution != null &&
                previousResultOutputImageResolution.Distance(outputImageResolution) < 20)
            {
                canUseOldFrames = true;
            }


            List<string> outputImageFiles = new List<string>();


            do
            {
                tasks.QueueLength = MaxWaifuTaskThreads;

                while (tasks.CanQueueTask && remainingImages.Count > 0)
                {
                    this._taskState = $"{numCompleted}/{extractResult.ExtractedFiles.Count} frames complete, {MaxWaifuTaskThreads} at a time";
                    InvokeTaskStateChanged();

                    int frameIdx = ++numStarted;
                    Logger.Debug("Starting frame {@FrameIndex}/{@NumFrames}", frameIdx, extractResult.ExtractedFiles.Count);

                    string nextImgPath = remainingImages.Dequeue();
                    string fileName = Path.GetFileName(nextImgPath);

                    string inputFramePath = Path.Combine(tempInputFolderPath, fileName);
                    string outputFramePath = Path.Combine(tempOutputFolderPath, fileName);

                    outputImageFiles.Add(outputFramePath);

                    if (File.Exists(outputFramePath))
                    {
                        Logger.Information("Found old output frame {FrameIndex} for {OutputAnimationPath}", frameIdx, OutputFilePath);

                        if (!canUseOldFrames)
                        {
                            Logger.Information("Not using previous output frame {FrameIndex} for {OutputAnimationPath} since they do not match the current target resolution", frameIdx, this.OutputFilePath);

                            Logger.Information("Current output resolution: {@CurrentOutputResolution}", outputImageResolution);
                            Logger.Information("Previous output resolution: {@OldOutputResolution}", previousResultOutputImageResolution);

                            File.Delete(outputFramePath);
                        }
                        else
                        {
                            var outputFrameRes = ImageHelper.GetImageResolution(outputFramePath);
                            if (outputFrameRes != outputImageResolution)
                            {
                                Logger.Information("Using previous output frame {FrameIndex} for {OutputAnimationPath} but the image resolution is *slightly* off, resizing..");
                                ImageHelper.ResizeImage(outputFramePath, outputImageResolution);
                            }

                            numCompleted += 1;

                            continue;
                        }
                    }

                    var imageTask = new ImageTask(inputFramePath, outputFramePath, this.OutputResolutionResolver, this.ConvertMode);
                    imageTask.TaskCompleted += (task) =>
                    {
                        Interlocked.Increment(ref numCompleted);
                        tasks.TryCompleteTask(task);
                    };

                    imageTask.TaskFaulted += (task, reason) =>
                    {
                        Logger.Debug("ImageTask failed for frame {@FrameIndex}/{@NumFrames} while upscaling {@InputFile} to {@OutputFile}", frameIdx, extractResult.ExtractedFiles.Count, inputFramePath, outputFramePath);
                    };

                    imageTask.StartTask(tempInputFolderPath, tempOutputFolderPath, waifu2xCaffePath, ffmpegPath);

                    tasks.TryQueueTask(imageTask);
                }

                numSubTasks = tasks.RunningTasks.Count;

                await Task.Delay(10);

            } while (numCompleted < extractResult.ExtractedFiles.Count && !_canceled);









            if (this._canceled)
            {
                Logger.Debug("Canceling frame upconversion");

                foreach (var task in tasks.RunningTasks)
                    task.CancelTask();

                while (tasks.RunningTasks.Count > 0)
                    await Task.Delay(1);

                this._isRunning = false;
                Logger.Debug("AnimationTask has been canceled");
                return false;
            }



            Logger.Information("Resizing output frames for {OutputAnimationPath} to have the same even dimensions");
            foreach (var image in outputImageFiles)
            {
                var imageSize = ImageHelper.GetImageResolution(image);
                if (imageSize == outputImageResolution)
                    continue;

                ImageHelper.ResizeImage(image, outputImageResolution);
            }


            this._taskState = $"Combining {extractResult.ExtractedFiles.Count} frames to {Path.GetExtension(OutputFilePath)}";
            InvokeTaskStateChanged();

            bool success = await CompileProcess.Run(InputFilePath, OutputFilePath, tempOutputFolderPath, extractResult.Fps);

            Cleanup();

            _isRunning = false;
            return success;
        }
    }
}
