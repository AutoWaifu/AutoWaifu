using AutoWaifu.Lib.Jobs;
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
    public class AnimationTask : WaifuTask
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
        
        bool _isRunning = false;
        public override bool IsRunning => this._isRunning;

        public void Cleanup()
        {
            Logger.Verbose("Trace");

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
            Logger.Verbose("Trace");
            this._canceled = true;

            while (this.IsRunning)
                await Task.Delay(1);

            Cleanup();

            return true;
        }

        protected override bool Dispose()
        {
            Cleanup();
            return true;
        }

        protected override bool Initialize()
        {
            return true;
        }

        protected override async Task<bool> Start(string tempInputFolderPath, string tempOutputFolderPath, string waifu2xCaffePath, string ffmpegPath)
        {
            Logger.Verbose("Trace");

            //  INIT
            _isRunning = true;
            _tempInputDir = tempInputFolderPath;

            this._taskState = "extracting animation frames";
            InvokeTaskStateChanged();

            AnimationFrameExtractor.Configure(this.InputFilePath, tempInputFolderPath);

            Logger.Debug("Extracting animation frames for {InputAnimation}", InputFilePath);

            //  EXTRACT FRAMES
            QueueJob(AnimationFrameExtractor);

            await AnimationFrameExtractor;

            Logger.Debug("Frame extraction ended");

            if (this._canceled)
            {
                Logger.Information("Terminating frame extraction for {AnimationPath} since this task has been cancelled", this.InputFilePath);
                _isRunning = false;
                return false;
            }

            var extractResult = AnimationFrameExtractor.Result;

            if (extractResult == null || AnimationFrameExtractor.State == JobState.Faulted)
            {
                Logger.Error("An error occurred while extracting the animation frames for {InputFilePath} using {TaskExtractorTypeName}", this.InputFilePath, AnimationFrameExtractor.GetType().Name);
                _isRunning = false;
                return false;
            }




            //  CHECK OUTPUT RESOLTION LIMITS
            Logger.Verbose("Checking output resolution limits on {ResolutionResolver}", this.OutputResolutionResolver.GetType().Name);
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

            //  CHECK PREVIOUS OUTPUT FRAMES ARE SAME SIZE
            Logger.Verbose("Checking current output against old output frame resolutions");

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


            //  FRAME UPSCALE
            Logger.Verbose("Starting frame upscale");
            
            InvokeTaskStateChanged();


            #region
            /*

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
                }
            }

    */
            #endregion


            var imageTasks = (from inputFileName in extractResult.ExtractedFiles
                              select new ImageTask(inputFilePath: Path.Combine(tempInputFolderPath, inputFileName),
                                                   outputFilePath: Path.Combine(tempOutputFolderPath, inputFileName),
                                                   outputResolutionResolver: outputResolutionResolver,
                                                   convertMode: ConvertMode)).ToArray();

            int frameIdx = 0;
            foreach (var task in imageTasks)
            {
                bool runTask = true;

                if (File.Exists(task.OutputFilePath))
                {
                    Logger.Information("Found old output frame {FrameIndex} for {OutputAnimationPath}", frameIdx, OutputFilePath);

                    if (!canUseOldFrames)
                    {
                        Logger.Information("Not using previous output frame {FrameIndex} for {OutputAnimationPath} since they do not match the current target resolution", frameIdx, this.OutputFilePath);

                        Logger.Information("Current output resolution: {@CurrentOutputResolution}", outputImageResolution);
                        Logger.Information("Previous output resolution: {@OldOutputResolution}", previousResultOutputImageResolution);

                        File.Delete(OutputFilePath);
                    }
                    else
                    {
                        var outputFrameRes = ImageHelper.GetImageResolution(task.OutputFilePath);
                        if (outputFrameRes != outputImageResolution)
                        {
                            Logger.Information("Using previous output frame {FrameIndex} for {OutputAnimationPath} but the image resolution is *slightly* off, resizing..");
                            ImageHelper.ResizeImage(task.OutputFilePath, outputImageResolution);
                        }

                        runTask = false;
                    }
                }

                if (runTask)
                    task.StartTask(tempInputFolderPath, tempOutputFolderPath, waifu2xCaffePath, ffmpegPath);

                frameIdx++;
            }

            Logger.Verbose("Finished launching ImageTasks and checking old frames");
            Logger.Verbose("Waiting for ImageTasks to complete and polling jobs");

            this._taskState = "Upscaling images";
            InvokeTaskStateChanged();

            while (imageTasks.Any(t => t.IsRunning))
            {
                var newTaskJobs = imageTasks.SelectMany(t => t.PollPendingJobs()).ToArray();
                if (newTaskJobs.Length > 0)
                {
                    Logger.Verbose("Found {NumSubJobs} jobs to queue", newTaskJobs.Length);
                    QueueJobs(newTaskJobs);
                }
                await Task.Delay(1).ConfigureAwait(false);
            }

            if (this._canceled)
                return false;

            if (imageTasks.Any(t => t.WasFaulted))
            {
                Logger.Error("An error occurred while processing one of the frames for {InputAnimationPath}", InputFilePath);
                return false;
            }

            Logger.Verbose("Finished waiting for image tasks, none faulted");

            IEnumerable<string> outputImageFiles = from inputFileName in extractResult.ExtractedFiles
                                                  select Path.Combine(tempOutputFolderPath, inputFileName);


            Logger.Debug("Resizing output frames for {OutputAnimationPath} to have even dimensions", OutputFilePath);

            var parallelResult = Parallel.ForEach(outputImageFiles, new ParallelOptions { MaxDegreeOfParallelism = 4 }, (file) =>
            {
                var imageSize = ImageHelper.GetImageResolution(file);
                if (imageSize == outputImageResolution)
                    return;

                ImageHelper.ResizeImage(file, outputImageResolution);
            });

            await TaskUtil.WaitUntil(() => parallelResult.IsCompleted).ConfigureAwait(false);

            this._taskState = $"Combining {extractResult.ExtractedFiles.Count} frames to {Path.GetExtension(OutputFilePath)}";
            InvokeTaskStateChanged();

            CompileProcess.Configure(InputFilePath, OutputFilePath, tempOutputFolderPath, extractResult.Fps);
            QueueJob(CompileProcess);
            await TaskUtil.WaitUntil(() => !CompileProcess.IsInQueue());

            if (CompileProcess.State != JobState.Completed)
            {
                Logger.Error("Failed to compile animation frames into {OutputFileExt} using {CompileProcessName}", Path.GetExtension(OutputFilePath), CompileProcess.GetType().Name);
                return false;
            }

            Cleanup();

            _isRunning = false;
            return true;
        }
    }
}
