using AutoWaifu.Lib.Cui;
using AutoWaifu.Lib.Cui.WaifuCaffe;
using Serilog;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AutoWaifu.Lib.Waifu2x.Tasks
{
    public class ImageTask : IWaifuTask
    {
        public ImageTask(string inputFilePath,
                         string outputFilePath,
                         IResolutionResolver outputResolutionResolver,
                         WaifuConvertMode convertMode) : base(outputResolutionResolver, convertMode)
        {
            InputFilePath = inputFilePath;
            OutputFilePath = outputFilePath;
        }

        public override IEnumerable<IWaifuTask> SubTasks => Enumerable.Empty<IWaifuTask>();

        public override int NumSubTasks => 0;

        public override bool IsRunning => upscaleTask != null;

        public override string InputFilePath { get; }
        public string OutputFilePath { get; }

        public override string TaskState => null;

        Task<bool> upscaleTask = null;
        bool terminate = false;



        internal WaifuCaffeOptions CustomTaskWaifuCaffeOptions;



        protected override Task<bool> Start(string tempInputPath, string tempOutputPath, string waifu2xCaffePath, string ffmpegPath)
        {
            return upscaleTask = Task.Run(async () =>
            {
                try
                {
                    var waifuCaffeInstance = new WaifuCaffeInstance(Path.Combine(waifu2xCaffePath, "waifu2x-caffe-cui.exe"));

                    if (CustomTaskWaifuCaffeOptions != null)
                    {
                        waifuCaffeInstance.Options = CustomTaskWaifuCaffeOptions;
                    }
                    else
                    {
                        waifuCaffeInstance.Options = new WaifuCaffeOptions
                        {
                            ConvertMode = this.ConvertMode,
                            ResolutionResolver = this.OutputResolutionResolver,
                            ProcessPriority = this.ProcessPriority
                        };
                    }



                    var waifuResult = await waifuCaffeInstance.Start(InputFilePath, OutputFilePath, () => this.terminate);

                    if (this.terminate)
                    {
                        Logger.Debug("ImageTask for {InputImagePath} has been canceled", InputFilePath);
                        return false;
                    }

                    Logger.Debug("Ran waifu2x-caffe with params {@WaifuArgs}", waifuResult.Args);

                    upscaleTask = null;

                    if (waifuResult.ExitCode == 1 || !File.Exists(OutputFilePath))
                    {
                        Logger.Error("Running waifu2x-caffe-cui on {@InputPath} failed, process terminated with exit code {@ExitCode}", InputFilePath, waifuResult.ExitCode);
                        Logger.Error("WaifuCaffe output for task {@InputPath}:\n{@StandardOutputStream}", InputFilePath, waifuResult.OutputStreamData);
                        return false;
                    }

                    Logger.Debug("Completed task for {@InputPath} with NoiseLevel={@NoiseLevel}", InputFilePath, waifuCaffeInstance.Options.NoiseLevel);

                    return true;
                }
                catch (Exception e)
                {
                    Logger.Error(e, "A top-level error occurred while running waifu2x-caffe-cui and waiting for its completion");
                    upscaleTask = null;
                    return false;
                }
            });
        }

        protected override Task<bool> Cancel()
        {
            return Task.Run<bool>(async () =>
            {
                if (!IsRunning)
                    return true;

                try
                {
                    this.terminate = true;
                    if (upscaleTask != null)
                        await upscaleTask;
                    return true;
                }
                catch
                {
                    return false;
                }
            });
        }






        protected override bool Initialize()
        {
            return true;
        }

        protected override bool Dispose()
        {
            return true;
        }
    }
}
