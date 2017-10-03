using AutoWaifu.Lib.Jobs;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AutoWaifu.Lib.Waifu2x.Tasks
{
    public interface IAnimationExtractor : IJob
    {
        string[] SupportedFileTypes { get; }

        AnimationExtractionResult Result { get; }

        /// <summary>
        /// All implementors of IAnimationExtractor should output images in the format {input-name}_NNNN.png, Returns the average framerate of the extracted animation.
        /// </summary>
        void Configure(string animationPath, string outputFolderPath);
    }
}
