using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AutoWaifu.Lib.Waifu2x.Tasks
{
    public interface IAnimationTaskCompileProcess
    {
        Task<bool> Run(string inputFilePath, string outputFilePath, string animFramesDirPath, double framerate);
    }
}
