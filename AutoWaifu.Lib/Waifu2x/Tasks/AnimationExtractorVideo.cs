using AutoWaifu.Lib.Cui.Ffmpeg;
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
    public class AnimationExtractorVideo : Job, IAnimationExtractor
    {
        public AnimationExtractorVideo(string ffmpegPath)
        {
            FfmpegPath = ffmpegPath;

            Exited += (j) => this.runningFfmpeg = null;
        }

        string animationPath;
        string outputFolderPath;
        FfmpegInstance runningFfmpeg;

        public string FfmpegPath { get; }

        public string[] SupportedFileTypes => new[] { ".mp4" };

        public AnimationExtractionResult Result { get; private set; }

        public override ResourceConsumptionLevel ResourceConsumption => ResourceConsumptionLevel.Medium;

        public void Configure(string animationPath, string outputFolderPath)
        {
            this.animationPath = animationPath;
            this.outputFolderPath = outputFolderPath;
        }

        async Task<AnimationExtractionResult> ExtractFrames(string animationPath, string outputFolderPath)
        {
            Logger.Verbose("Trace");

            string animationName = Path.GetFileNameWithoutExtension(animationPath);

            this.runningFfmpeg = new FfmpegInstance(FfmpegPath);
            this.runningFfmpeg.Options = new FfmpegRawOptions { RawParams = $"-i \"{animationPath}\" \"{outputFolderPath}\\{animationName}_%04d.png\"" };

            await this.runningFfmpeg.Run().ConfigureAwait(false);
            var runInfo = this.runningFfmpeg.Result;
            this.runningFfmpeg = null;

            if (this.TerminateRequested)
                return null;

            if (runInfo.ExitCode != 0)
            {
                Logger.Error("Running video frame extraction on {VideoPath} failed, where ffmpeg output: {FfmpegConsoleOutput}", animationPath, string.Join("\n", runInfo.OutputStreamData));
                return null;
            }

            Logger.Verbose("Successfully ran ffmpeg, capturing video FPS from ffmpeg output data");

            var ffmpegOutput = string.Join("\n", runInfo.OutputStreamData);

            var fpsMatcher = new Regex(@"(\d+) fps");
            var fpsMatch = fpsMatcher.Match(ffmpegOutput);
            var fpsString = fpsMatch.Groups[1].Value;
            double fps = double.Parse(fpsString);

            var durationMatcher = new Regex(@"Duration\: (\d+\:\d+\:\d+\.\d+)");
            var durationString = durationMatcher.Match(ffmpegOutput).Captures[0].Value;
            var durationParts = durationString.Split(new[] { ':', '.' });

            TimeSpan duration = new TimeSpan(0, int.Parse(durationParts[1]),
                                                int.Parse(durationParts[2]),
                                                int.Parse(durationParts[3]),
                                                int.Parse(durationParts[4]));

            Logger.Verbose("Detected video is {0} seconds long", duration.TotalSeconds);

            int numFrames = (int)Math.Round(duration.TotalSeconds * fps);

            Logger.Verbose("Detected framerate is {0}fps", numFrames);

            Logger.Verbose("Modifying output ffmpeg frames files from 1-indexed to 0-indexed");

            List<string> outputFiles = new List<string>();
            for (int i = 1; i <= numFrames; i++)
            {
                if (this.TerminateRequested || this.SuspendRequested)
                    break;

                string originalIdxString = i.ToString();
                originalIdxString = new string('0', 4 - originalIdxString.Length) + originalIdxString;

                string correctedIdxString = (i - 1).ToString();
                correctedIdxString = new string('0', 4 - correctedIdxString.Length) + correctedIdxString;

                string originalFile = $"{outputFolderPath}\\{animationName}_{originalIdxString}.png";
                string correctedFile = $"{outputFolderPath}\\{animationName}_{correctedIdxString}.png";
                File.Move(originalFile, correctedFile);
                

                outputFiles.Add(correctedFile);
            }

            State = JobState.Completed;

            return new AnimationExtractionResult
            {
                Fps = fps,
                ExtractedFiles = outputFiles
            };
        }

        protected override Task DoRun()
        {
            Logger.Verbose("Trace");

            if (this.animationPath == null)
                throw new ArgumentNullException($"Tried to start a {nameof(AnimationExtractorVideo)} without configuring the input animation path");

            if (this.outputFolderPath == null)
                throw new ArgumentNullException($"Tried to start a {nameof(AnimationExtractorVideo)} without configuring the output folder path");

            return ExtractFrames(this.animationPath, this.outputFolderPath);
        }

        protected override Task DoTerminate()
        {
            Logger.Verbose("Trace");

            return TaskUtil.WaitUntil(() => IsRunExecuting == false);
        }
    }
}
