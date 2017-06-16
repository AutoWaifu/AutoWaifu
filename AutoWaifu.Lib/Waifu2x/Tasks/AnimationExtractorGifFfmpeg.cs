using AutoWaifu.Lib.Cui.Ffmpeg;
using ImageMagick;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace AutoWaifu.Lib.Waifu2x.Tasks
{
    public class AnimationExtractorGifFfmpeg : Loggable, IAnimationExtractor
    {
        public AnimationExtractorGifFfmpeg(string ffmpegPath)
        {
            FfmpegPath = ffmpegPath;
        }

        public int DenoiseAmount { get; set; } = 1;

        public string FfmpegPath { get; }

        public string[] SupportedAnimationTypes => new[] { ".gif" };

        public async Task<AnimationExtractionResult> ExtractFrames(string animationPath, string outputFolderPath, Func<bool> shouldTerminateDelegate = null)
        {
            string animationName = Path.GetFileNameWithoutExtension(animationPath);
            string outputFramesFormat = Path.Combine(outputFolderPath, animationName) + "_%04d.png";

            double framerate = 0;

            using (MagickImageCollection gifFrames = new MagickImageCollection(animationPath))
            {
                foreach (var frame in gifFrames)
                {
                    if (shouldTerminateDelegate())
                        return null;

                    framerate += frame.AnimationDelay / 100.0 / gifFrames.Count;
                }
            }

            framerate = 1.0 / framerate;

            var ffmpegInstance = new FfmpegInstance(FfmpegPath);
            ffmpegInstance.Options = new FfmpegRawOptions
            {
                RawParams = $"-i \"{animationPath}\" -vf fps={framerate} \"{outputFramesFormat}\""
            };

            var ffmpegResult = await ffmpegInstance.Start(null, null, shouldTerminateDelegate);
            if (shouldTerminateDelegate())
                return null;

            if (ffmpegResult.ExitCode != 0)
            {
                Logger.Error("Failed to extract GIF frames for {InputAnimationPath} with ffmpeg, ffmpeg output was {@FfmpegOutput}", animationPath, ffmpegResult.OutputStreamData);
                return null;
            }
            
            var animationFiles = Directory.EnumerateFiles(outputFolderPath).Where(f => f.Contains(animationName));


            if (DenoiseAmount > 0)
            {
                Logger.Verbose("Frame extraction complete, denoising by {DenoiseAmount}x", DenoiseAmount);

                foreach (var frame in animationFiles)
                {
                    if (shouldTerminateDelegate())
                        return null;

                    using (MagickImage img = new MagickImage(frame))
                    {
                        for (int i = 0; i < DenoiseAmount; i++)
                            img.Despeckle();

                        img.Write(frame);
                    }
                }
            }

            return new AnimationExtractionResult
            {
                Fps = framerate,
                ExtractedFiles = animationFiles.ToList()
            };
        }
    }
}
