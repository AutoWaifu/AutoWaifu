using AutoWaifu.Lib.Cui.Ffmpeg;
using AutoWaifu.Lib.Jobs;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AutoWaifu.Lib.Waifu2x.Tasks
{
    public class AnimationTaskCompileProcessGif : Job, IAnimationTaskCompileProcess
    {
        public AnimationTaskCompileProcessGif(string ffmpegPath)
        {
            FfmpegPath = ffmpegPath;

            this.Exited += (j) =>
            {
                this.runningInstance = null;
                this.innerCompileProcess = null;
            };
        }

        string inputFilePath, outputFilePath, animFramesDirPath;
        double framerate = -1;

        public string FfmpegPath { get; }

        public int MaxOutputResolutionMegapixels => 1000;

        public override ResourceConsumptionLevel ResourceConsumption => throw new NotImplementedException();

        FfmpegInstance runningInstance;
        IAnimationTaskCompileProcess innerCompileProcess;

        async Task Compile(string inputFilePath, string outputFilePath, string animFramesDirPath, double framerate)
        {
            //  General process:
            //  - Compile frames to temp mp4
            //  - Generate GIF palette from mp4
            //  - Convert temp mp4 to GIF using palette
            //  - Delete temp mp4
            Logger.Verbose("Trace");

            if (this.runningInstance != null)
            {
                throw new InvalidOperationException($"Tried to run a {nameof(AnimationTaskCompileProcessGif)} when it was already running");
            }

            this.innerCompileProcess = new AnimationTaskCompileProcessVideo(FfmpegPath, new FfmpegCompatibilityOptions
            {
                TargetCompatibility = FfmpegCompatibilityOptions.OutputCompatibilityType.HighQualityLowCompatibility,
                OutputFramerate = (int)framerate
            });

            string tmpOutputFilePath = $"{Path.GetDirectoryName(outputFilePath)}/{Path.GetFileName(outputFilePath)}.mp4";

            this.innerCompileProcess.Configure(this.inputFilePath, this.outputFilePath, this.animFramesDirPath, this.framerate);


            if (File.Exists(tmpOutputFilePath))
                File.Delete(tmpOutputFilePath);

            await this.innerCompileProcess.Run().ConfigureAwait(false);

            if (this.innerCompileProcess.State != JobState.Completed)
            {
                this.innerCompileProcess = null;
                State = JobState.Faulted;
                return;
            }

            this.innerCompileProcess = null;

            if (TerminateRequested)
                return;

            string outputPaletteFile = $"{Path.GetDirectoryName(outputFilePath)}\\{Path.GetFileNameWithoutExtension(outputFilePath)}_palette.png";

            this.runningInstance = new FfmpegInstance(FfmpegPath);
            this.runningInstance.Options = new FfmpegRawOptions
            {
                RawParams = $"-i \"{tmpOutputFilePath}\" -vf palettegen \"{outputPaletteFile}\""
            };

            await this.runningInstance.Run().ConfigureAwait(false);
            var paletteResult = this.runningInstance.Result;
            this.runningInstance = null;

            if (TerminateRequested)
                return;

            if (paletteResult.ExitCode != 0)
            {
                Logger.Error("ffmpeg failed while generating a GIF palette for {InputFilePath}, where ffmpeg output {FfmpegConsoleOutputStream}", inputFilePath, string.Join("\n", paletteResult.OutputStreamData));
                State = JobState.Faulted;
                return;
            }

            this.runningInstance = new FfmpegInstance(FfmpegPath);
            this.runningInstance.Options = new FfmpegRawOptions
            {
                RawParams = $"-i \"{tmpOutputFilePath}\" -i \"{outputPaletteFile}\" -lavfi \"paletteuse\" \"{outputFilePath}\""
            };

            await this.runningInstance.Run().ConfigureAwait(false);
            var ffmpegResult = this.runningInstance.Result;
            this.runningInstance = null;

            if (TerminateRequested)
                return;

            if (ffmpegResult.ExitCode != 0)
            {
                Logger.Error("ffmpeg failed while converting a temporary mp4 to gif for {InputFilePath}, where ffmpeg output {FfmpegConsoleOutputStream}", inputFilePath, string.Join("\n", ffmpegResult.OutputStreamData));
                File.Delete(outputPaletteFile);
                State = JobState.Faulted;
                return;
            }

            File.Delete(outputPaletteFile);
            File.Delete(tmpOutputFilePath);

            if (File.Exists(outputFilePath))
                State = JobState.Completed;
            else
                State = JobState.Faulted;
        }

        protected override Task DoRun()
        {
            Logger.Verbose("Trace");

            if (this.inputFilePath == null)
                throw new ArgumentNullException($"Tried to run {nameof(AnimationTaskCompileProcessGif)} without configuring {nameof(inputFilePath)} first");

            if (this.outputFilePath == null)
                throw new ArgumentNullException($"Tried to run {nameof(AnimationTaskCompileProcessGif)} without configuring {nameof(outputFilePath)} first");

            if (this.animFramesDirPath == null)
                throw new ArgumentNullException($"Tried to run {nameof(AnimationTaskCompileProcessGif)} without configuring {nameof(animFramesDirPath)} first");

            if (this.framerate <= 0)
                throw new ArgumentNullException($"Tried to run {nameof(AnimationTaskCompileProcessGif)} when {nameof(framerate)} <= 0 (currently {this.framerate})");

            return Compile(this.inputFilePath, this.outputFilePath, this.animFramesDirPath, this.framerate);
        }

        protected override async Task DoTerminate()
        {
            Logger.Verbose("Trace");

            if (this.runningInstance != null)
                await this.runningInstance.Terminate().ConfigureAwait(false);

            if (this.innerCompileProcess != null)
                await this.innerCompileProcess.Terminate().ConfigureAwait(false);
        }

        public void Configure(string inputFilePath, string outputFilePath, string animFramesDirPath, double framerate)
        {
            this.inputFilePath = inputFilePath;
            this.outputFilePath = outputFilePath;
            this.animFramesDirPath = animFramesDirPath;
            this.framerate = framerate;
        }
    }
}
