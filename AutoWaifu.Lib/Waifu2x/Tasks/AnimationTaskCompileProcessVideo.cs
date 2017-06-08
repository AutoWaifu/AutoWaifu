using AutoWaifu.Lib.Cui.Ffmpeg;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AutoWaifu.Lib.Waifu2x.Tasks
{
    public class AnimationTaskCompileProcessVideo : Loggable, IAnimationTaskCompileProcess
    {
        public AnimationTaskCompileProcessVideo(string ffmpegPath, IFfmpegOptions ffmpegOptions)
        {
            FfmpegPath = ffmpegPath;
            FfmpegOptions = ffmpegOptions;
        }

        public string FfmpegPath { get; }

        public IFfmpegOptions FfmpegOptions { get; }

        public bool Cancel { get; set; } = false;

        public async Task<bool> Run(string inputFilePath, string outputFilePath, string animFramesDirPath, double framerate)
        {
            Logger.Verbose("Running ffmpeg to combine frames from {AnimFramesDir} into {OutputFile}", animFramesDirPath, outputFilePath);

            var ffmpegInstance = new FfmpegInstance(FfmpegPath);
            FfmpegOptions.OutputFramerate = (int)framerate;
            ffmpegInstance.Options = FfmpegOptions;

            string inputPathFormat = Path.Combine(animFramesDirPath, $"{Path.GetFileNameWithoutExtension(inputFilePath)}_%04d.png");
            var ffmpegResult = await ffmpegInstance.Start(inputPathFormat, outputFilePath, () => this.Cancel);

            Logger.Verbose("Ran ffmpeg with parameters: {FfmpegParameters}", ffmpegResult.Args);

            if (ffmpegResult.ExitCode != 0 && !ffmpegResult.WasTerminated)
            {
                Logger.Error("Error occurred while running ffmpeg {@FfmpegOutput}", ffmpegResult.OutputStreamData);
                return false;
            }

            return true;
        }
    }
}
