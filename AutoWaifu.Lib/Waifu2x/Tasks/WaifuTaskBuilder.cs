using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AutoWaifu.Lib.Waifu2x
{
    public class WaifuTaskBuilder
    {
        public WaifuTaskBuilder(IResolutionResolver outputResolutionResolver, WaifuConvertMode convertMode)
        {
            OutputResolutionResolver = outputResolutionResolver;
            ConvertMode = convertMode;
        }

        public IResolutionResolver OutputResolutionResolver { get; }
        public WaifuConvertMode ConvertMode { get; }

        public bool CanMakeTaskFor(string inputFilePath)
        {
            var ext = Path.GetExtension(inputFilePath);

            return ext == ".png" ||
                   ext == ".jpg" ||
                   ext == ".jpeg" ||
                   ext == ".gif";
        }

        public IWaifuTask TaskFor(string inputFilePath, string outputFilePath)
        {
            //if (Path.GetExtension(inputFilePath) != Path.GetExtension(outputFilePath))
            //    throw new InvalidOperationException("Input and output file extensions do not match");

            if (!CanMakeTaskFor(inputFilePath))
                throw new InvalidOperationException($"Cannot make IWaifuTask for '{inputFilePath}', file type not supported");

            var ext = Path.GetExtension(inputFilePath);

            switch (ext)
            {
                case ".jpg":
                    goto case ".png";

                case ".jpeg":
                    goto case ".png";

                case ".png":
                    return new ImageTask(inputFilePath, outputFilePath, OutputResolutionResolver, ConvertMode);

                case ".gif":
                    return new GifTask(inputFilePath, outputFilePath, OutputResolutionResolver, ConvertMode);

                default:
                    return null;

            }
        }
    }
}
