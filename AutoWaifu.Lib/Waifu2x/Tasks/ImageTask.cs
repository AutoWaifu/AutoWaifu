using AutoWaifu.Lib.Cui;
using AutoWaifu.Lib.Cui.WaifuCaffe;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using WaifuLog;

namespace AutoWaifu.Lib.Waifu2x
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





        protected override Task<bool> Start(string tempInputPath, string tempOutputPath, string waifu2xCaffePath, string ffmpegPath)
        {
            return upscaleTask = Task.Run(async () =>
            {
                try
                {
                    var waifuCaffeInstance = new WaifuCaffeInstance(Path.Combine(waifu2xCaffePath, "waifu2x-caffe-cui.exe"));
                    waifuCaffeInstance.Options = new WaifuCaffeOptions
                    {
                        ConvertMode = this.ConvertMode,
                        ResolutionResolver = this.OutputResolutionResolver,
                        ProcessPriority = this.ProcessPriority
                    };

                    var waifuResult = await waifuCaffeInstance.Start(InputFilePath, OutputFilePath, () => this.terminate);

                    WaifuLogger.Info($"Ran waifu2x-caffe with params {waifuResult.Args}");

                    upscaleTask = null;

                    if (waifuResult.ExitCode == 1 || !File.Exists(OutputFilePath))
                    {
                        WaifuLogger.ExternalError($"Running waifu2x-caffe-cui on {InputFilePath} failed, process terminated with exit code {waifuResult.ExitCode}");
                        WaifuLogger.ExternalError($"WaifuCaffe output for task {InputFilePath}:\n{string.Join("\n", waifuResult.OutputStreamData)}");
                        return false;
                    }

                    WaifuLogger.Info($"Completed task for {InputFilePath}");

                    return true;
                }
                catch (Exception e)
                {
                    WaifuLogger.Exception($"A top-level error occurred while running waifu2x-caffe-cui and waiting for its completion", e);
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
