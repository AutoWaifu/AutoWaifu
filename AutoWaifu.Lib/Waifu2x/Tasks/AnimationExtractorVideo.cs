using AutoWaifu.Lib.Cui.Ffmpeg;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace AutoWaifu.Lib.Waifu2x.Tasks
{
    public class AnimationExtractorVideo : IAnimationExtractor
    {
        public AnimationExtractorVideo(string ffmpegPath)
        {
            FfmpegPath = ffmpegPath;
        }

        public string FfmpegPath { get; }

        public string[] SupportedAnimationTypes => new[] { ".mp4" };

        public async Task<AnimationExtractionResult> ExtractFrames(string animationPath, string outputFolderPath)
        {
            string animationName = Path.GetFileNameWithoutExtension(animationPath);

            var ffmpegInstance = new FfmpegInstance(FfmpegPath);
            ffmpegInstance.Options = new FfmpegRawOptions { RawParams = $"-i \"{animationPath}\" \"{outputFolderPath}\\{animationName}_%04d.png\"" };

            var runInfo = await ffmpegInstance.Start(null, null);
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

            int numFrames = (int)Math.Round(duration.TotalSeconds * fps);

            List<string> outputFiles = new List<string>();
            for (int i = 1; i <= numFrames; i++)
            {
                string originalIdxString = i.ToString();
                originalIdxString = new string('0', 4 - originalIdxString.Length) + originalIdxString;

                string correctedIdxString = (i - 1).ToString();
                correctedIdxString = new string('0', 4 - correctedIdxString.Length) + correctedIdxString;

                string originalFile = $"{outputFolderPath}\\{animationName}_{originalIdxString}.png";
                string correctedFile = $"{outputFolderPath}\\{animationName}_{correctedIdxString}.png";
                File.Move(originalFile, correctedFile);
                

                outputFiles.Add(correctedFile);
            }

            return new AnimationExtractionResult
            {
                Fps = fps,
                ExtractedFiles = outputFiles
            };
        }
    }
}
