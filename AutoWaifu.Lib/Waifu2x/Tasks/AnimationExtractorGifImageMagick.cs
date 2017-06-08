using ImageMagick;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AutoWaifu.Lib.Waifu2x.Tasks
{
    public class AnimationExtractorGifImageMagick : IAnimationExtractor
    {
        public string[] SupportedAnimationTypes => new[] { ".gif" };

        public int DespeckleAmount { get; set; }

        public async Task<AnimationExtractionResult> ExtractFrames(string animationPath, string outputFolderPath)
        {
            return await Task.Run(() =>
            {
                using (MagickImageCollection collection = new MagickImageCollection(animationPath))
                {
                    var frames = collection.ToArray();
                    double averageMs = frames.Select(f => f.AnimationDelay * 10).Sum() / (double)frames.Length;
                    double avgFps = 1000 / averageMs;

                    string animationName = Path.GetFileNameWithoutExtension(animationPath);

                    List<string> outputFiles = new List<string>();

                    int frameIndex = 0;
                    foreach (var frame in frames)
                    {
                        string idxString = (frameIndex++).ToString();
                        idxString = new string('0', 4 - idxString.Length) + idxString;
                        string outputFile = $"{Path.Combine(outputFolderPath, animationName)}_{idxString}.png";

                        outputFiles.Add(outputFile);
                        
                        //for (int i = 0; i < DespeckleAmount; i++)
                        //    frame.Despeckle();

                        frame.Format = MagickFormat.Png;
                        frame.Write(outputFile);
                    }

                    return new AnimationExtractionResult
                    {
                        ExtractedFiles = outputFiles,
                        Fps = avgFps
                    };
                }
            });
        }
    }
}
