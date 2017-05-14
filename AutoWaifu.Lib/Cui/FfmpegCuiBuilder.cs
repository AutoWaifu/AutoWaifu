using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AutoWaifu.Lib.Cui
{
    public class FfmpegCuiBuilder
    {
        public struct Results
        {
            public string Params;
            public string OutputFile;
        }

        public static Results CuiParamsFor(int fps, string inputFolder, string inputFileNameBase, string ext, string outputFolder = null)
        {
            throw new NotImplementedException();

            //if (outputFolder == null)
            //    outputFolder = AppSettings.Main.OutputDir;

            //if (inputFolder == null)
            //    inputFolder = AppSettings.Main.InputDir;
            

            StringBuilder builder = new StringBuilder();
            var filePath = Path.Combine(inputFolder, inputFileNameBase);
            filePath += "%04d" + ext;

            builder.Append(" -r " + fps);
            builder.Append(" -i ");
            builder.Append(" \"" + filePath + "\" ");
            builder.Append(" -vcodec h264 ");
            builder.Append(" -crf 15 ");



            string outputFile = Path.GetFullPath(Path.Combine(outputFolder, inputFileNameBase + ".mp4"));
            builder.Append(" \"" + outputFile + "\" ");

            if (File.Exists(outputFile))
                File.Delete(outputFile);

            return new Results
            {
                Params = builder.ToString(),
                OutputFile = outputFile
            };
        }
    }
}
