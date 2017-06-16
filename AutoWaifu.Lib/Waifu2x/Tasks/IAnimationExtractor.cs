using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AutoWaifu.Lib.Waifu2x.Tasks
{
    public interface IAnimationExtractor
    {
        string[] SupportedAnimationTypes { get; }

        /// <summary>
        /// All implementors of ExtractFrames should output images in the format {input-name}_NNNNNN.{png}. Returns the average framerate of the extracted animation.
        /// </summary>
        Task<AnimationExtractionResult> ExtractFrames(string animationPath, string outputFolderPath, Func<bool> shouldTerminageDelegate = null);
    }
}
