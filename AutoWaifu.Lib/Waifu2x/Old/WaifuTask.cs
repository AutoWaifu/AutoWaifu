using ImageMagick;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;

namespace AutoWaifu.Lib.Waifu2x.Old
{
    public class WaifuTask
    {
        //#region Performance-tracking Logic
        //DateTime _startTime;
        //bool _completed = false;
        //bool _isStarted = false;

        //public PerformanceTracking.TimePoint PerformanceData
        //{
        //    get;
        //    private set;
        //}


        //void MarkTaskComplete(DateTime completionTime)
        //{
        //    var img = System.Drawing.Image.FromFile(OutputFile);
        //    OutputFileSize = new System.Windows.Size(img.Width, img.Height);


        //    PerformanceData = new PerformanceTracking.TimePoint();

        //    PerformanceData.ConvertDuration = completionTime - _startTime;
        //    PerformanceData.TaskConfig = new PerformanceTracking.TaskConfiguration
        //    {
        //        ConvertMode = this.ConvertMode,

        //        InputImageWidth = (int)InputFileSize.Width,
        //        InputImageHeight = (int)InputFileSize.Height,
        //        OutputImageWidth = (int)OutputFileSize.Width,
        //        OutputImageHeight = (int)OutputFileSize.Height
        //    };

        //    switch (ImageType)
        //    {
        //        case WaifuImageType.Jpeg:
        //            PerformanceData.TaskConfig.ImageType = WaifuImageType.Jpeg;
        //            break;

        //        case WaifuImageType.Png:
        //            PerformanceData.TaskConfig.ImageType = WaifuImageType.Png;
        //            break;
        //    }
        //}



        //void MarkTaskStarted(DateTime startTime)
        //{
        //    if (_isStarted)
        //        throw new Exception();

        //    _isStarted = true;
        //    _startTime = startTime;
        //}

        //#endregion

        //public bool Completed => _completed;

        //public InputMetricCacheItem LoadForInput(string path)
        //{
        //    bool FileIsReadable(string file)
        //    {
        //        try
        //        {
        //            File.OpenRead(file).Dispose();
        //            return true;
        //        }
        //        catch (Exception e)
        //        {
        //            return false;
        //        }
        //    }

        //    while (!FileIsReadable(path))
        //        Task.Delay(200).Wait();

        //    var image = System.Drawing.Image.FromFile(path);
        //    InputFileSize = new System.Windows.Size(image.Width, image.Height);
        //    InputFile = path;

        //    var cacheItem = new InputMetricCacheItem
        //    {
        //        FullPath = path,
        //        Height = (uint)InputFileSize.Width,
        //        Width = (uint)InputFileSize.Height
        //    };

        //    var dateModified = new FileInfo(path).LastWriteTime;
        //    cacheItem.LastModifiedDate = dateModified;

        //    return cacheItem;
        //}

        //public InputMetricCacheItem LoadForInputCache(InputMetricCacheItem cacheItem, FileInfo currentFileInfo)
        //{
        //    if (currentFileInfo.LastWriteTime != cacheItem.LastModifiedDate)
        //        return LoadForInput(cacheItem.FullPath);

        //    InputFileSize = new System.Windows.Size(cacheItem.Width, cacheItem.Height);
        //    InputFile = cacheItem.FullPath;

        //    return null;
        //}

        List<Task> _pendingTasks = new List<Task>();
        bool _terminate = false;
        public void Teriminate()
        {
            if (WasTerminated)
                return;

            _terminate = true;
            while (_pendingTasks.Count > 0)
            {
                var task = _pendingTasks[0];
                task.Wait();
                _pendingTasks.Remove(task);
            }

            WasTerminated = true;
        }

        public bool WasTerminated
        {
            get;
            private set;
        }

        /// <summary>
        /// Whether or not the task should actually do work
        /// </summary>
        public bool IsTestRun { get; set; } = false;


        Task<bool> StartImage(string inputFile, string outputFile)
        {
            
        }

        Task<bool> StartGif(string fullPath)
        {
#pragma warning disable CS1998 // Async method lacks 'await' operators and will run synchronously
            return Task.Run(async () =>
            {
                List<string> frameFiles = new List<string>();
                int numFrames;
                int frameLength = -1;

                string gifName = Path.GetFileName(fullPath);
                Console.WriteLine("WaifuTask.StartGif for {0}", fullPath);

                try
                {
                    using (MagickImageCollection collection = new MagickImageCollection(fullPath))
                    {
                        collection.Coalesce();

                        numFrames = collection.Count;

                        Console.WriteLine("Extracting {0} frames for {1} to {2}", numFrames, gifName, AppSettings.Main.TempDirInput);

                        int idx = 0;
                        foreach (var img in collection)
                        {
                            frameLength = img.AnimationDelay * 10;

                            string idxString = (idx++).ToString();
                            if (idxString.Length < 2)
                                idxString = "000" + idxString;
                            if (idxString.Length < 3)
                                idxString = "00" + idxString;
                            if (idxString.Length < 4)
                                idxString = "0" + idxString;

                            string frameFile = Path.Combine(AppSettings.Main.TempDirInput, $"{Path.GetFileName(fullPath)}_{idxString}.jpg");

                            img.Format = MagickFormat.Jpeg;
                            img.Write(frameFile);

                            frameFiles.Add(frameFile);
                        }
                    }
                }
                catch (Exception e)
                {
                    Console.WriteLine("An error occurred");
                    Console.WriteLine(e);
                    return false;
                }

                Console.WriteLine("Frame-length = {0}ms", frameLength);

                Console.WriteLine("Finished extraction, beginning upconvert...");

                //MessageBox.Show("Gif processing for " + numFrames + " frames");


                List <string> outputImages = new List<string>();
                foreach (var file in frameFiles)
                {
                    var outputFrameFile = Path.Combine(AppSettings.Main.TempDirOutput, Path.GetFileName(file));


                    var outputParams = WaifuCaffeCuiBuilder.CuiParamsFor(file, outputFrameFile);
                    outputImages.Add(outputParams.OutputFile);

                    //Console.WriteLine("Starting upconvert of frame {0}/{1} for {2}", outputImages.Count, numFrames, gifName);
                    //Console.WriteLine("waifu2x-caffe-cui params: {0}", outputParams.Params);

                    var imageTask = StartImage(file, outputFrameFile);
                    _pendingTasks.Add(imageTask);
                    await imageTask;
                    _pendingTasks.Remove(imageTask);

                    if (!IsTestRun)
                        ResizeEvenDims(new[] { outputFrameFile });

                    Console.WriteLine("Completed frame {0}/{1} for {2}", outputImages.Count, numFrames, gifName);

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

                Console.WriteLine("Completed upscale for all {0} frames of {1}, setting up ffmpeg for mp4 generation using constant frame-length of {2}ms per frame", numFrames, Path.GetFileName(fullPath), frameLength);

                int fps = (int)(1.0 / (frameLength / 1000.0));
                Console.WriteLine("FPS = {0}", fps);

                var ffmpegParams = FfmpegCuiBuilder.CuiParamsFor(fps, Path.GetDirectoryName(outputImages.First()), $"{Path.GetFileName(fullPath)}_", ".jpg", AppSettings.Main.OutputDir);

                Console.WriteLine("Starting ffmpeg...");
                Console.WriteLine("ffmpeg params: " + ffmpegParams.Params);

                try
                {
                    var psi = new ProcessStartInfo
                    {
                        Arguments = ffmpegParams.Params,
                        FileName = "ffmpeg.exe",
                        CreateNoWindow = true,
                        RedirectStandardError = true,
                        RedirectStandardOutput = true,
                        RedirectStandardInput = true,
                        WorkingDirectory = ".",
                        UseShellExecute = false
                    };

                    var process = new Process() { StartInfo = psi };
                    process.OutputDataReceived += (sender, args) => Console.WriteLine(args.Data);
                    process.Start();
                    //while (!process.HasExited)
                    //    await Task.Delay(1);
                }
                catch (Exception e)
                {
                    Console.WriteLine("An error occurred");
                    Console.WriteLine(e);
                    return false;
                }

                Console.WriteLine("ffmpeg done, GIF complete.");

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

                if (newWidth == img.Width && newHeight == img.Height)
                    continue;

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

                    newImg.Save(file);
                }
            }
        }

        public Task Start(string inputFile, string outputFile)
        {
            MarkTaskStarted(DateTime.Now);

            this.InputFile = inputFile;

            var task = Task.Run(async () =>
            {

                if (ImageType == WaifuImageType.Gif)
                {
                    if (await StartGif(inputFile))
                        MarkTaskComplete(DateTime.Now);
                }
                else
                {
                    

                    var imageTask = StartImage(inputFile, outputFile);
                    _pendingTasks.Add(imageTask);
                    if (await imageTask)
                        MarkTaskComplete(DateTime.Now);
                }
            });

            return task;
        }

        public Task StartSpecific(string inputFile, string outputFile)
        {
            MarkTaskStarted(DateTime.Now);

            this.InputFile = inputFile;

            var task = Task.Run(async () =>
            {
                var cuiResult = WaifuCaffeCuiBuilder.CuiParamsFor(inputFile, outputFile);
                this.OutputFile = cuiResult.OutputFile;

                if (ImageType == WaifuImageType.Gif)
                {
                    if (await StartGif(inputFile))
                        MarkTaskComplete(DateTime.Now);
                }
                else
                {


                    var imageTask = StartImage(inputFile, outputFile);
                    _pendingTasks.Add(imageTask);
                    if (await imageTask)
                        MarkTaskComplete(DateTime.Now);
                }
            });

            return task;
        }
    }
}
