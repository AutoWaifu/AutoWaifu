using AutoWaifu.Lib.Waifu2x;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Management;
using System.Text;
using System.Threading.Tasks;

namespace AutoWaifu.Lib.Cui
{
    public class WaifuCaffeCuiBuilder
    {
        static int NumParamsBuilt = 0;

        public WaifuCaffeCuiBuilder(IResolutionResolver outputResResolver, WaifuConvertMode convertMode)
        {
            ResolutionResolver = outputResResolver;
            ConvertMode = convertMode;
        }

        public int GpuIndex = 0;
        public IResolutionResolver ResolutionResolver;
        public WaifuConvertMode ConvertMode;

        public struct Results
        {
            public string Params;
            public string OutputFile;
        }

        public Results GetCuiParams(string inputFilePath, string outputFilePath)
        {
            StringBuilder builder = new StringBuilder();

            inputFilePath = Path.GetFullPath(inputFilePath);
            outputFilePath = Path.GetFullPath(outputFilePath);

            using (var img = Image.FromFile(inputFilePath))
            {
                builder.Append($" -i \"{inputFilePath}\" ");
                var desiredResolution = ResolutionResolver.Resolve(new ImageResolution
                {
                    Width = img.Width,
                    Height = img.Height
                });

                    int widthDifference = desiredResolution.WidthInt - img.Width;
                    int heightDifference = desiredResolution.HeightInt - img.Height;

                    if (widthDifference > heightDifference)
                    {
                        builder.Append(" -w " + desiredResolution.WidthInt + " ");
                    }
                    else
                    {
                        builder.Append(" -h " + desiredResolution.HeightInt + " ");
                    }
            }



            builder.Append(" -e jpg ");

            builder.Append(" -p ");
            switch (ConvertMode)
            {
                case WaifuConvertMode.CPU:
                    builder.Append("cpu");
                    break;

                case WaifuConvertMode.GPU:
                    builder.Append("gpu");
                    builder.Append($" -gpu {GpuIndex} ");
                    break;

                case WaifuConvertMode.cuDNN:
                    builder.Append("cudnn");
                    builder.Append($" -gpu {GpuIndex} ");
                    break;
            }


            if (inputFilePath.Contains(".gif"))
            {
                builder.Append(" -n 3 ");
            }
            else
            {
                builder.Append(" -n 1 ");
            }


            builder.Append(" -b 4 ");


            builder.Append(" -o \"" + outputFilePath + "\" ");

            ++NumParamsBuilt;
            return new Results
            {
                Params = builder.ToString(),
                OutputFile = outputFilePath
            };
        }

        //public static Results CuiParamsFor(string fullFilePath, string inputFolder = null, string outputFolder = null)
        //{
        //    if (outputFolder == null)
        //        outputFolder = AppSettings.Main.OutputDir;

        //    if (inputFolder == null)
        //        inputFolder = AppSettings.Main.InputDir;

        //    if (NumGPUs < 0)
        //    {
        //        ManagementClass c = new ManagementClass("Win32_VideoController");
        //        NumGPUs = c.GetInstances().Count;
        //    }

        //    StringBuilder builder = new StringBuilder();
        //    //builder.Append(Path.Combine(AppSettings.Main.Waifu2xCaffeDir, "Waifuwx-caffe-cui.exe "));

        //    var filePath = Path.Combine(inputFolder, fullFilePath);
        //    var img = System.Drawing.Image.FromFile(filePath);


        //    if (AppSettings.Main.UseScaleInsteadOfSize)
        //    {
        //        builder.Append(" -s " + AppSettings.Main.Scale + " ");
        //    }
        //    else
        //    {
        //        if (img.Width > img.Height)
        //        {
        //            builder.Append(" -w " + AppSettings.Main.DesiredWidth + " ");
        //        }
        //        else
        //        {
        //            builder.Append(" -h " + AppSettings.Main.DesiredHeight + " ");
        //        }
        //    }

        //    builder.Append(" -i \"" + filePath + "\" ");
        //    builder.Append(" -e png ");

        //    builder.Append(" -p ");
        //    int gpuIndex = NumParamsBuilt % NumGPUs;
        //    switch (AppSettings.Main.ConversionMode)
        //    {
        //        case AppSettings.WaifuConvertMode.CPU:
        //            builder.Append("cpu");
        //            break;

        //        case AppSettings.WaifuConvertMode.GPU:
        //            builder.Append("gpu");
        //            builder.Append($" -gpu {gpuIndex} ");
        //            break;

        //        case AppSettings.WaifuConvertMode.cuDNN:
        //            builder.Append("cudnn");
        //            builder.Append($" -gpu {gpuIndex} ");
        //            break;
        //    }


        //    if (fullFilePath.Contains(".gif"))
        //    {
        //        builder.Append(" -n 3 ");
        //    }
        //    else
        //    {
        //        builder.Append(" -n 1 ");
        //    }


        //    builder.Append(" -b 4 ");



        //    string outputFile = Path.GetFullPath(Path.Combine(outputFolder, fullFilePath));
        //    builder.Append(" -o \"" + outputFile + "\" ");

        //    ++NumParamsBuilt;
        //    return new Results
        //    {
        //        Params = builder.ToString(),
        //        OutputFile = outputFile
        //    };
        //}
    }
}
