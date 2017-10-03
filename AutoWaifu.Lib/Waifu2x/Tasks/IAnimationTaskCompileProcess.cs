using AutoWaifu.Lib.Jobs;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AutoWaifu.Lib.Waifu2x.Tasks
{
    public interface IAnimationTaskCompileProcess : IJob
    {
        void Configure(string inputFilePath, string outputFilePath, string animFramesDirPath, double framerate);

        int MaxOutputResolutionMegapixels { get; }
    }
}
