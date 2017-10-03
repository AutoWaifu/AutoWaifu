using AutoWaifu.Lib.Cui;
//using AutoWaifu.Lib.Cui.WaifuCaffe;
using AutoWaifu.Lib.Cui.WaifuCV;
using AutoWaifu.Lib.Jobs;
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
    public class ImageTask : WaifuTask
    {
        public ImageTask(string inputFilePath,
                         string outputFilePath,
                         IResolutionResolver outputResolutionResolver,
                         WaifuConvertMode convertMode) : base(outputResolutionResolver, convertMode)
        {
            InputFilePath = inputFilePath;
            OutputFilePath = outputFilePath;
        }
        

        public override bool IsRunning => upscaleTask != null;

        public override string InputFilePath { get; }
        public string OutputFilePath { get; }

        public override string TaskState => null;

        Task<bool> upscaleTask = null;
        bool terminate = false;
        WaifuCVInstance waifu2xInstance = null;



        internal Waifu2xOptions CustomTaskWaifuCaffeOptions;
        

        protected override Task<bool> Start(string tempInputPath, string tempOutputPath, string waifu2xCaffePath, string ffmpegPath)
        {
            Logger.Verbose("Trace");

            return upscaleTask = Task.Run(async () =>
            {
                try
                {
                    this.waifu2xInstance = new WaifuCVInstance(Path.Combine(waifu2xCaffePath, "waifu2x-converter_x64.exe"));

                    if (CustomTaskWaifuCaffeOptions != null)
                    {
                        this.waifu2xInstance.Options = CustomTaskWaifuCaffeOptions;
                    }
                    else
                    {
                        this.waifu2xInstance.Options = new Waifu2xOptions
                        {
                            OutputResolutionResolver = this.OutputResolutionResolver,
                            ProcessPriority = this.ProcessPriority
                        };
                    }

                    this.waifu2xInstance.Configure(InputFilePath, OutputFilePath);

                    var inputResolution = ImageHelper.GetImageResolution(InputFilePath);
                    var outputResolution = this.waifu2xInstance.Options.OutputResolutionResolver.Resolve(inputResolution);

                    Logger.Debug("Upscaling {ImagePath} from {InputWidth}x{InputHeight} to {OutputWidth}x{OutputHeight}",
                                                InputFilePath,
                                                inputResolution.WidthInt, inputResolution.HeightInt,
                                                outputResolution.WidthInt, outputResolution.HeightInt);


                    //  RUN WAIFU2X
                    QueueJob(this.waifu2xInstance);
                    await this.waifu2xInstance;

                    var waifuResult = this.waifu2xInstance.Result;

                    if (this.terminate)
                    {
                        Logger.Debug("ImageTask for {InputImagePath} has been canceled", InputFilePath); 
                        return false;
                    }

                    if (this.waifu2xInstance.State == JobState.Faulted)
                        throw new Exception("The waifu2x upscale job faulted");

                    if (this.waifu2xInstance.State != JobState.Completed)
                        return false;

                    this.waifu2xInstance = null;

                    upscaleTask = null;

                    if (waifuResult.ExitCode == 1 || !File.Exists(OutputFilePath))
                    {
                        Logger.Error("Running waifu2x-caffe-cui on {@InputPath} failed, process terminated with exit code {@ExitCode}", InputFilePath, waifuResult.ExitCode);
                        Logger.Error("WaifuCaffe output for task {@InputPath}:\n{@StandardOutputStream}", InputFilePath, string.Join("\n", waifuResult.OutputStreamData));
                        return false;
                    }

                    Logger.Debug("Completed task for {@InputPath} with NoiseLevel={@NoiseLevel}", InputFilePath, this.waifu2xInstance.Options.NoiseLevel);

                    return true;
                }
                catch (Exception e)
                {
                    Logger.Error(e, "A top-level error occurred while running waifu2x-caffe-cui and waiting for its completion");
                    upscaleTask = null;
                    return false;
                }
                finally
                {
                    this.waifu2xInstance = null;
                }
            });
        }

        protected override Task<bool> Cancel()
        {
            Logger.Verbose("Trace");

            return Task.Run<bool>(async () =>
            {
                if (!IsRunning)
                    return true;

                try
                {
                    this.terminate = true;

                    if (this.waifu2xInstance != null)
                        await this.waifu2xInstance.Terminate().ConfigureAwait(false);

                    if (upscaleTask != null)
                        await upscaleTask.ConfigureAwait(false);

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
