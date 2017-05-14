using AutoWaifu.Lib.Cui;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

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






        Task<bool> upscaleTask = null;
        bool terminate = false;





        protected override Task<bool> Start(string waifu2xCaffePath, string ffmpegPath)
        {
            return upscaleTask = Task.Run(() =>
            {
                try
                {
                    var cuiBuilder = new WaifuCaffeCuiBuilder(OutputResolutionResolver, ConvertMode);
                    var cuiResult = cuiBuilder.GetCuiParams(InputFilePath, OutputFilePath);
                    var waifuExe = Path.Combine(waifu2xCaffePath, "waifu2x-caffe-cui.exe");
                    var startInfo = new ProcessStartInfo();
                    startInfo.FileName = waifuExe;
                    startInfo.Arguments = cuiResult.Params;
                    startInfo.WorkingDirectory = "waifu2x-caffe";
                    startInfo.CreateNoWindow = true;
                    startInfo.WindowStyle = ProcessWindowStyle.Hidden;
                    startInfo.RedirectStandardInput = true;
                    startInfo.RedirectStandardOutput = true;
                    startInfo.RedirectStandardError = true;
                    startInfo.UseShellExecute = false;
                    var process = Process.Start(startInfo);
                    process.PriorityClass = ProcessPriority;

                    while (!process.HasExited)
                    {
                        process.WaitForExit(100);
                        if (this.terminate)
                            process.Kill();
                    }

                    upscaleTask = null;

                    if (process.ExitCode == 1 || !File.Exists(cuiResult.OutputFile))
                        return false;

                    return true;
                }
                catch (Exception e)
                {
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
