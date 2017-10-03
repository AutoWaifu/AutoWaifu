using ImageMagick;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using AutoWaifu.Lib.Jobs;

namespace AutoWaifu.Lib.Waifu2x.Tasks
{
    public class AnimationExtractorGifImageMagick : Job, IAnimationExtractor
    {
        string animationPath, outputFolderPath;

        public string[] SupportedFileTypes => new[] { ".gif" };

        public int DespeckleAmount { get; set; }

        public override ResourceConsumptionLevel ResourceConsumption => ResourceConsumptionLevel.Low;

        public AnimationExtractionResult Result { get; private set; }

        public void Configure(string animationPath, string outputFolderPath)
        {
            this.animationPath = animationPath;
            this.outputFolderPath = outputFolderPath;
        }

        protected override async Task DoRun()
        {
            Logger.Verbose("Trace");
            if (this.animationPath == null)
                throw new ArgumentNullException($"Tried to run {nameof(AnimationExtractorGifImageMagick)} without configuring {nameof(animationPath)}");

            if (this.outputFolderPath == null)
                throw new ArgumentNullException($"Tried to run {nameof(AnimationExtractorGifImageMagick)} without configuring {nameof(outputFolderPath)}");

            Result = await ExtractFrames(this.animationPath, this.outputFolderPath);
        }

        protected override Task DoTerminate()
        {
            return TaskUtil.WaitUntil(() => IsRunExecuting == false);
        }

        async Task<AnimationExtractionResult> ExtractFrames(string animationPath, string outputFolderPath)
        {
            Logger.Verbose("Trace");
            return await Task.Run(() =>
            {
                using (MagickImageCollection collection = new MagickImageCollection(animationPath))
                {
                    var frames = collection.ToArray();
                    double averageMs = frames.Select(f => f.AnimationDelay * 10).Sum() / (double)frames.Length;
                    double avgFps = 1000 / averageMs;

                    Logger.Verbose("Used ImageMagick to detect the animation {AnimationName} at {AnimationFps}fps (this may be inaccurate for gifs with varying frame durations)", animationPath, avgFps);

                    string animationName = Path.GetFileNameWithoutExtension(animationPath);

                    List<string> outputFiles = new List<string>();

                    if (DespeckleAmount > 0)
                    {
                        Logger.Verbose("Denoising output frames by {DenoiseAmt}x", DespeckleAmount);
                    }

                    int frameIndex = 0;
                    foreach (var frame in frames)
                    {
                        if (TerminateRequested)
                            return null;

                        string idxString = (frameIndex++).ToString();   
                        idxString = new string('0', 4 - idxString.Length) + idxString;
                        string outputFile = $"{Path.Combine(outputFolderPath, animationName)}_{idxString}.png";

                        outputFiles.Add(outputFile);
                        
                        for (int i = 0; i < DespeckleAmount; i++)
                            frame.Despeckle();

                        frame.Format = MagickFormat.Png;
                        frame.Write(outputFile);
                    }

                    State = JobState.Completed;

                    return new AnimationExtractionResult
                    {
                        ExtractedFiles = outputFiles,
                        Fps = avgFps
                    };
                }
            }).ConfigureAwait(false);
        }
    }
}
