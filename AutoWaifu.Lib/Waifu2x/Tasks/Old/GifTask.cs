using AutoWaifu.Lib.Cui;
using AutoWaifu.Lib.Cui.Ffmpeg;
using ImageMagick;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AutoWaifu.Lib.Waifu2x
{
    public class GifTask : IWaifuTask
    {

        public string OutputFilePath { get; }
        public override string InputFilePath { get; }

        public override string TaskState
        {
            get
            {
                return $"{numSubTasks}/{numSubTasksCompleted}";
            }
        }

        List<ImageTask> imageTasks = new List<ImageTask>();
        public override IEnumerable<IWaifuTask> SubTasks => imageTasks;

        int numSubTasks = -1;
        int numSubTasksCompleted = 0;
        public override int NumSubTasks => numSubTasks;

        bool isRunning = false;
        public override bool IsRunning => isRunning;

        bool _terminate = false;
        Task<bool> _runningTask = null;

        public GifTask(string inputFilePath, string outputFilePath, IResolutionResolver resolutionResolver, WaifuConvertMode convertMode) : base(resolutionResolver, convertMode)
        {
            this.InputFilePath = inputFilePath;
            this.OutputFilePath = outputFilePath;
        }

        Task<bool> StartGif(string inputPath, string outputPath, string tempInputPath, string tempOutputPath, string waifu2xCaffePath, string ffmpegPath)
        {
#pragma warning disable CS1998 // Async method lacks 'await' operators and will run synchronously
            return Task.Run(async () =>
            {
                List<string> frameFiles = new List<string>();
                int numFrames;
                double frameLength = 0;

                string gifName = Path.GetFileName(InputFilePath);
                WaifuLogger.Info($"WaifuTask.StartGif for {inputPath}");

                try
                {
                    WaifuLogger.Info($"Loading {inputPath}...");

                    using (MagickImageCollection collection = new MagickImageCollection(inputPath))
                    {
                        collection.Coalesce();

                        numFrames = collection.Count;

                        WaifuLogger.Info($"Extracting {numFrames} frames for {gifName} to {tempInputPath}");

                        int idx = 0;
                        foreach (var img in collection)
                        {
                            if (this._terminate)
                                return false;

                            frameLength += img.AnimationDelay * 10.0  / collection.Count;

                            string idxString = (idx++).ToString();
                            if (idxString.Length < 2)
                                idxString = "000" + idxString;
                            if (idxString.Length < 3)
                                idxString = "00" + idxString;
                            if (idxString.Length < 4)
                                idxString = "0" + idxString;

                            string frameFile = Path.Combine(tempInputPath, $"{Path.GetFileName(InputFilePath)}_{idxString}.jpeg");

                            WaifuLogger.Info($"Saving frame {idx}/{collection.Count}");

                            img.Format = MagickFormat.Jpeg;
                            img.Write(frameFile);

                            frameFiles.Add(frameFile);
                        }
                    }
                }
                catch (Exception e)
                {
                    WaifuLogger.Exception(e);
                    return false;
                }

                WaifuLogger.Info($"Frame-length = {frameLength}ms");

                WaifuLogger.Info("Finished extraction, beginning upconvert...");



                //  Check that resolution doesn't exceed h264 max resolution
                var outputResolutionResolver = this.OutputResolutionResolver;

                ImageResolution imageSize;
                using (var image = Image.FromFile(frameFiles.First()))
                {
                    imageSize = new ImageResolution
                    {
                        WidthInt = image.Width,
                        HeightInt = image.Height
                    };
                }

                var outputResolution = outputResolutionResolver.Resolve(imageSize);
                double outputPixelCount = outputResolution.Width * outputResolution.Height;
                if (outputPixelCount > 8000000) // Max is ~8MP
                {
                    WaifuLogger.Warning($"Upscale resolution for {this.InputFilePath} exceeds h264 limit of 8 megapixels (currently targeting {outputPixelCount / 1e6} megapixels), limiting output resolution");
                    outputResolutionResolver = new MegapixelResolutionResolver(8e6f);
                }



                List<string> outputImages = new List<string>();
                foreach (var file in frameFiles)
                {
                    var outputFrameFile = Path.Combine(tempOutputPath, Path.GetFileName(file));

                    if (File.Exists(outputFrameFile))
                    {
                        WaifuLogger.Info($"Found existing frame for {gifName} at {outputFrameFile}");
                    }
                    else
                    {
                        var imageTask = new ImageTask(file, outputFrameFile, this.OutputResolutionResolver, this.ConvertMode);
                        imageTask.CustomTaskWaifuCaffeOptions = new Cui.WaifuCaffe.WaifuCaffeOptions
                        {
                            ConvertMode = this.ConvertMode,
                            ProcessPriority = this.ProcessPriority,
                            ResolutionResolver = outputResolutionResolver,
                            NoiseLevel = Cui.WaifuCaffe.WaifuCaffeOptions.ImageNoiseLevel.High
                        };

                        imageTasks.Add(imageTask);
                        await imageTask.StartTask(tempInputPath, tempOutputPath, waifu2xCaffePath, ffmpegPath);
                        imageTasks.Remove(imageTask);

                        ResizeEvenDims(new[] { outputFrameFile });

                        WaifuLogger.Info($"Completed frame {outputImages.Count + 1}/{numFrames} for {gifName}");
                    }

                    outputFrameFile = ImageTypeHelper.RectifyImageExtension(outputFrameFile);
                    outputImages.Add(outputFrameFile);

                    InvokeTaskStateChanged();

                    if (_terminate)
                        break;
                }


                if (_terminate)
                {
                    foreach (var frame in frameFiles)
                        File.Delete(frame);

                    foreach (var frame in outputImages)
                    {
                        if (File.Exists(frame))
                            File.Delete(frame);
                    }

                    return false;
                }
                

                WaifuLogger.Info($"Completed upscale for all {numFrames} frames of {Path.GetFileName(InputFilePath)}, setting up ffmpeg for mp4 generation using constant frame-length of {(int)frameLength}ms per frame");

                int fps = (int)(1.0 / (frameLength / 1000.0));
                WaifuLogger.Info($"FPS = {fps}");

                if (File.Exists(outputPath))
                    File.Delete(outputPath);

                var ffmpegInstance = new FfmpegInstance(ffmpegPath);
                ffmpegInstance.Options = new FfmpegCompatibilityOptions
                {
                    OutputFramerate = fps,
                    TargetCompatibility = FfmpegCompatibilityOptions.OutputCompatibilityType.HighQualityLowCompatibility
                };

                WaifuLogger.Info("Starting ffmpeg...");
                string inputImagePathFormat = Path.Combine(tempOutputPath, Path.GetFileName(InputFilePath) + "_%4d" + Path.GetExtension(outputImages.First()));
                var ffmpegResult = await ffmpegInstance.Start(inputImagePathFormat, OutputFilePath, () => this._terminate);
                
                WaifuLogger.Info("Ran ffmpeg with params: " + ffmpegResult.Args);

                if (ffmpegResult.ExitCode != 0)
                {
                    WaifuLogger.ExternalError($"Running ffmpeg on {InputFilePath} failed, process terminated with exit code {ffmpegResult.ExitCode}");
                    WaifuLogger.ExternalError($"ffmpeg output for task {InputFilePath}:\n{string.Join("\n", ffmpegResult.OutputStreamData)}");
                    return false;
                }
                

                WaifuLogger.Info("ffmpeg done, GIF complete.");

                return true;
            });
#pragma warning restore CS1998 // Async method lacks 'await' operators and will run synchronously
        }
        static void ResizeEvenDims(string[] files)
        {
            foreach (var file in files)
            {
                var img = Bitmap.FromStream(new MemoryStream(File.ReadAllBytes(file)));
                

                int newWidth = img.Width + (img.Width % 2);
                int newHeight = img.Height + (img.Height % 2);

                using (var newImg = new Bitmap(newWidth, newHeight))
                {
                    using (Graphics g = Graphics.FromImage(newImg))
                    {
                        g.CompositingMode = System.Drawing.Drawing2D.CompositingMode.SourceCopy;
                        g.CompositingQuality = System.Drawing.Drawing2D.CompositingQuality.HighQuality;
                        g.InterpolationMode = System.Drawing.Drawing2D.InterpolationMode.HighQualityBicubic;
                        g.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.HighQuality;
                        g.PixelOffsetMode = System.Drawing.Drawing2D.PixelOffsetMode.HighQuality;

                        using (var wrapMode = new ImageAttributes())
                        {
                            var dstRect = new Rectangle(0, 0, newWidth, newHeight);

                            wrapMode.SetWrapMode(System.Drawing.Drawing2D.WrapMode.TileFlipXY);
                            g.DrawImage(img, dstRect, 0, 0, newWidth, newHeight, GraphicsUnit.Pixel, wrapMode);
                        }
                    }

                    File.Delete(file);
                    newImg.Save(Path.Combine(Path.GetDirectoryName(file), Path.GetFileNameWithoutExtension(file) + ".jpeg"));
                }
            }
        }

        protected override async Task<bool> Start(string tempInputFolderPath, string tempOutputFolderPath, string waifu2xCaffePath, string ffmpegPath)
        {
            this.isRunning = true;
            _runningTask = StartGif(this.InputFilePath, this.OutputFilePath, tempInputFolderPath, tempOutputFolderPath, waifu2xCaffePath, ffmpegPath);
            bool result = await _runningTask;
            this.isRunning = false;
            return result;
        }

        protected override async Task<bool> Cancel()
        {
            _terminate = true;

            foreach (var task in this.imageTasks)
                task.CancelTask();

            while (IsRunning)
                await Task.Delay(1);

            return true;
        }

        protected override bool Initialize()
        {
            //  TODO - Gif frame extraction
            return true;
        }

        protected override bool Dispose()
        {
            //  TODO - Delete temp data
            return true;
        }
    }
}
