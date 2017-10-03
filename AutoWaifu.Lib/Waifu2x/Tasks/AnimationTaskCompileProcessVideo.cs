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
    public class AnimationTaskCompileProcessVideo : Job, IAnimationTaskCompileProcess
    {
        public AnimationTaskCompileProcessVideo(string ffmpegPath, IFfmpegOptions ffmpegOptions)
        {
            FfmpegPath = ffmpegPath;
            FfmpegOptions = ffmpegOptions;

            this.Exited += (j) => ffmpegInstance = null;
        }

        FfmpegInstance ffmpegInstance;
        string inputFilePath, outputFilePath, animFramesDirPath;
        double framerate;

        public string FfmpegPath { get; }

        public IFfmpegOptions FfmpegOptions { get; }
        

        public int MaxOutputResolutionMegapixels => 8;

        public override ResourceConsumptionLevel ResourceConsumption => ResourceConsumptionLevel.Medium;

        async Task Compile(string inputFilePath, string outputFilePath, string animFramesDirPath, double framerate)
        {
            Logger.Verbose("Running ffmpeg to combine frames from {AnimFramesDir} into {OutputFile}", animFramesDirPath, outputFilePath);

            this.ffmpegInstance = new FfmpegInstance(FfmpegPath);
            FfmpegOptions.OutputFramerate = (int)framerate;
            this.ffmpegInstance.Options = FfmpegOptions;

            string inputPathFormat = Path.Combine(animFramesDirPath, $"{Path.GetFileNameWithoutExtension(inputFilePath)}_%04d.png");

            await this.ffmpegInstance.Run().ConfigureAwait(false);
            var ffmpegResult = this.ffmpegInstance.Result;
            this.ffmpegInstance = null;
            
            if (ffmpegResult.ExitCode != 0 && !ffmpegResult.WasTerminated)
            {
                State = JobState.Faulted;
                Logger.Error("Error occurred while running ffmpeg {@FfmpegOutput}", ffmpegResult.OutputStreamData);
                return;
            }

            State = JobState.Completed;
        }

        protected override Task DoRun()
        {
            if (this.inputFilePath == null)
                throw new ArgumentNullException($"Tried to run {nameof(AnimationTaskCompileProcessVideo)} without configuring {nameof(inputFilePath)}");

            if (this.outputFilePath == null)
                throw new ArgumentNullException($"Tried to run {nameof(AnimationTaskCompileProcessVideo)} without configuring {nameof(outputFilePath)}");

            if (this.animFramesDirPath == null)
                throw new ArgumentNullException($"Tried to run {nameof(AnimationTaskCompileProcessVideo)} without configuring {nameof(animFramesDirPath)}");

            if (this.framerate <= 0)
                throw new ArgumentNullException($"Tried to run {nameof(AnimationTaskCompileProcessVideo)} with {nameof(framerate)} <= 0 (currently {this.framerate})");

            return Compile(this.inputFilePath, this.outputFilePath, this.animFramesDirPath, this.framerate);
        }
        
        protected override async Task DoTerminate()
        {
            if (this.ffmpegInstance != null)
                await this.ffmpegInstance.Terminate().ConfigureAwait(false);

            await TaskUtil.WaitUntil(() => IsRunExecuting == false).ConfigureAwait(false);
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
