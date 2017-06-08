using AutoWaifu.Lib.Cui.Ffmpeg;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AutoWaifu.Lib.Waifu2x.Tasks
{
    public class AnimationTaskCompileProcessGif : Loggable, IAnimationTaskCompileProcess
    {
        public AnimationTaskCompileProcessGif(string ffmpegPath)
        {
            FfmpegPath = ffmpegPath;
        }

        public string FfmpegPath { get; }

        public async Task<bool> Run(string inputFilePath, string outputFilePath, string animFramesDirPath, double framerate)
        {
            var videoProcess = new AnimationTaskCompileProcessVideo(FfmpegPath, new FfmpegCompatibilityOptions
            {
                TargetCompatibility = FfmpegCompatibilityOptions.OutputCompatibilityType.HighQualityLowCompatibility,
                OutputFramerate = (int)framerate
            });


            string tmpOutputFilePath = $"{Path.GetDirectoryName(outputFilePath)}/{Path.GetFileName(outputFilePath)}.mp4";

            if (File.Exists(tmpOutputFilePath))
                File.Delete(tmpOutputFilePath);

            if (!await videoProcess.Run(inputFilePath, tmpOutputFilePath, animFramesDirPath, framerate))
                return false;

            string outputPaletteFile = $"{Path.GetDirectoryName(outputFilePath)}\\{Path.GetFileNameWithoutExtension(outputFilePath)}_palette.png";

            var paletteFfmpeg = new FfmpegInstance(FfmpegPath);
            paletteFfmpeg.Options = new FfmpegRawOptions
            {
                RawParams = $"-i \"{tmpOutputFilePath}\" -vf palettegen \"{outputPaletteFile}\""
            };

            var paletteResult = await paletteFfmpeg.Start(null, null);

            if (paletteResult.ExitCode != 0)
            {
                Logger.Error("ffmpeg failed while generating a GIF palette for {InputFilePath}, where ffmpeg output {FfmpegConsoleOutputStream}", inputFilePath, string.Join("\n", paletteResult.OutputStreamData));
                return false;
            }

            var videoFfmpeg = new FfmpegInstance(FfmpegPath);
            videoFfmpeg.Options = new FfmpegRawOptions
            {
                RawParams = $"-i \"{tmpOutputFilePath}\" -i \"{outputPaletteFile}\" -lavfi \"paletteuse\" \"{outputFilePath}\""
            };
            
            var ffmpegResult = await videoFfmpeg.Start(null, outputFilePath);

            if (ffmpegResult.ExitCode != 0)
            {
                Logger.Error("ffmpeg failed while converting a temporary mp4 to gif for {InputFilePath}, where ffmpeg output {FfmpegConsoleOutputStream}", inputFilePath, string.Join("\n", ffmpegResult.OutputStreamData));
                File.Delete(outputPaletteFile);
                return false;
            }

            File.Delete(outputPaletteFile);
            File.Delete(tmpOutputFilePath);

            return File.Exists(outputFilePath);
        }
    }
}
