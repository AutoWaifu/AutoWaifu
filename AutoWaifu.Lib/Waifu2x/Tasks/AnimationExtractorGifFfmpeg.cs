using AutoWaifu.Lib.Cui.Ffmpeg;
using ImageMagick;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using AutoWaifu.Lib.Jobs;

namespace AutoWaifu.Lib.Waifu2x.Tasks
{
    public class AnimationExtractorGifFfmpeg : Job, IAnimationExtractor
    {
        public AnimationExtractorGifFfmpeg(string ffmpegPath)
        {
            FfmpegPath = ffmpegPath;

            this.Exited += (j) => ffmpegInstance = null;
        }

        FfmpegInstance ffmpegInstance;
        string animationPath, outputFolderPath;

        public int DenoiseAmount { get; set; } = 1;

        public string FfmpegPath { get; }

        public string[] SupportedFileTypes => new[] { ".gif" };
        
        public override ResourceConsumptionLevel ResourceConsumption => ResourceConsumptionLevel.Medium;

        public AnimationExtractionResult Result { get; private set; }

        async Task Extract(string animationPath, string outputFolderPath)
        {
            Logger.Verbose("Trace");

            string animationName = Path.GetFileNameWithoutExtension(animationPath);
            string outputFramesFormat = Path.Combine(outputFolderPath, animationName) + "_%04d.png";

            double framerate = 0;

            using (MagickImageCollection gifFrames = new MagickImageCollection(animationPath))
            {
                foreach (var frame in gifFrames)
                {
                    if (TerminateRequested)
                        return;

                    framerate += frame.AnimationDelay / 100.0 / gifFrames.Count;
                }
            }

            //  TODO - use ffmpeg to get FPS, it reports the fps to use with its extracted frames

            framerate = 1.0 / framerate;

            Logger.Verbose("Detected {AnimationPath} to be {Fps}fps (this may be inaccurate until ffmpeg is used for fps detection)", animationPath, framerate);

            this.ffmpegInstance = new FfmpegInstance(FfmpegPath);
            this.ffmpegInstance.Options = new FfmpegRawOptions
            {
                RawParams = $"-i \"{animationPath}\" -vf fps={framerate} \"{outputFramesFormat}\""
            };

            await this.ffmpegInstance.Run().ConfigureAwait(false);
            var ffmpegResult = this.ffmpegInstance.Result;

            this.ffmpegInstance = null;

            if (TerminateRequested)
                return;

            if (ffmpegResult.ExitCode != 0)
            {
                Logger.Error("Failed to extract GIF frames for {InputAnimationPath} with ffmpeg, ffmpeg output was {@FfmpegOutput}", animationPath, ffmpegResult.OutputStreamData);
                return;
            }
            
            var animationFiles = Directory.EnumerateFiles(outputFolderPath).Where(f => Path.GetFileName(f).StartsWith(animationName + "_")).ToList();

            Logger.Verbose("ffmpeg extraction successful, renaming files to be 0-indexed instead of 1-indexed");

            List<string> outputFiles = new List<string>();
            for (int i = 1; i <= animationFiles.Count; i++)
            {
                if (TerminateRequested)
                    return;

                string originalIdxString = i.ToString();
                originalIdxString = new string('0', 4 - originalIdxString.Length) + originalIdxString;

                string correctedIdxString = (i - 1).ToString();
                correctedIdxString = new string('0', 4 - correctedIdxString.Length) + correctedIdxString;

                string originalFile = $"{outputFolderPath}\\{animationName}_{originalIdxString}.png";
                string correctedFile = $"{outputFolderPath}\\{animationName}_{correctedIdxString}.png";
                File.Move(originalFile, correctedFile);


                outputFiles.Add(correctedFile);
            }

            if (DenoiseAmount > 0)
            {
                Logger.Debug("Frame extraction complete, denoising by {DenoiseAmount}x", DenoiseAmount);

                foreach (var frame in outputFiles)
                {
                    if (TerminateRequested)
                        return;

                    using (MagickImage img = new MagickImage(frame))
                    {
                        for (int i = 0; i < DenoiseAmount; i++)
                        {
                            if (TerminateRequested)
                                return;
                            img.Despeckle();
                        }

                        img.Write(frame);
                    }
                }
            }

            Result = new AnimationExtractionResult
            {
                Fps = framerate,
                ExtractedFiles = outputFiles
            };

            State = JobState.Completed;
        }

        public void Configure(string animationPath, string outputFolderPath)
        {
            Logger.Verbose("Trace");
            this.animationPath = animationPath;
            this.outputFolderPath = outputFolderPath;
        }

        protected override Task DoRun()
        {
            Logger.Verbose("Trace");
            if (this.animationPath == null)
                throw new ArgumentNullException($"Tried to run {nameof(AnimationExtractorGifFfmpeg)} without configuring {nameof(animationPath)}");

            if (this.outputFolderPath == null)
                throw new ArgumentNullException($"Tried to run {nameof(AnimationExtractorGifFfmpeg)} without configuring {nameof(outputFolderPath)}");

            return Extract(this.animationPath, this.outputFolderPath);
        }

        protected override async Task DoTerminate()
        {
            Logger.Verbose("Trace");
            if (this.ffmpegInstance != null)
                await this.ffmpegInstance.Terminate().ConfigureAwait(false);

            await TaskUtil.WaitUntil(() => IsRunExecuting == false).ConfigureAwait(false);
        }
    }
}
