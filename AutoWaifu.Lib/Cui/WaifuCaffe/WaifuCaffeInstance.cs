using AutoWaifu.Lib.Jobs;
using AutoWaifu.Lib.Waifu2x;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AutoWaifu.Lib.Cui.WaifuCaffe
{
    class WaifuCaffeInstance : Job
    {
        public WaifuCaffeInstance(string waifuCaffePath)
        {
            WaifuCaffePath = waifuCaffePath;
        }

        public string WaifuCaffePath { get; }
        string inputFilePath, outputFilePath;

        public override ResourceConsumptionLevel ResourceConsumption => throw new NotImplementedException();

        public Waifu2xOptions Options;

        ProcessWrapper process;

        public struct RunInfo
        {
            public string Args;
            public string[] OutputStreamData;
            public int ExitCode;
            public bool WasTerminated;
        }

        public RunInfo Result { get; private set; }
        public void Configure(string inputFilePath, string outputFilePath)
        {
            this.inputFilePath = inputFilePath;
            this.outputFilePath = outputFilePath;
        }

        void RegisterLine(List<string> allLines, string line)
        {
            lock (allLines)
            {
                allLines.Add(line);
            }
        }

        string GetCuiParams(string inputFilePath, string outputFilePath)
        {
            List<string> paramParts = new List<string>();

            inputFilePath = Path.GetFullPath(inputFilePath);
            outputFilePath = Path.GetFullPath(outputFilePath);

            paramParts.Add($"-i \"{inputFilePath}\" -s {Options.GetOutputScale(inputFilePath)}");

            paramParts.Add("-p");
            switch (Options.ConvertMode)
            {
                case WaifuConvertMode.CPU:
                    paramParts.Add("cpu");
                    break;

                case WaifuConvertMode.GPU:
                    paramParts.Add("gpu");
                    paramParts.Add($"-gpu {Options.GpuIndex}");
                    break;

                case WaifuConvertMode.cuDNN:
                    paramParts.Add("cudnn");
                    paramParts.Add($"-gpu {Options.GpuIndex}");
                    break;
            }

            paramParts.Add($"-n {(int)Options.NoiseLevel}");


            paramParts.Add($"-o \"{outputFilePath}\"");

            return string.Join(" ", paramParts);
        }
        

        async Task<RunInfo> Start(string inputFilePath, string outputFilePath)
        {
            if (this.process != null)
                throw new InvalidOperationException("Cannot start a WaifuCaffe instance that's already running");

            if (WaifuCaffePath == null || !File.Exists(WaifuCaffePath))
            {
                Logger.Error("Cannot start WaifuCaffe for {@InputPath} since the specified WaifuCaffe path is invalid! (Either not set or the file doesn't exist!)", inputFilePath);
                return new RunInfo { ExitCode = -1 };
            }

            if (Options == null)
            {
                Logger.Error("Cannot start WaifuCaffe for {@InputPath} when Options is null!", inputFilePath);
                return new RunInfo { ExitCode = -1 };
            }

            if (this.process != null)
            {
                Logger.Warning("Cannot start WaifuCaffeInstance after it's already started");
                return new RunInfo { ExitCode = -1 };
            }

            var runInfo = new RunInfo();

            var @params = GetCuiParams(inputFilePath, outputFilePath);

            Logger.Debug("Running waifu2x-caffe-cui with params: {Waifu2xCaffeParams}", @params);
            runInfo.Args = @params;

            this.process = new ProcessWrapper
            {
                ProgramPath = WaifuCaffePath,
                CommandlineArgs = @params
            };

            var processTask = this.process.Start().ConfigureAwait(false).GetAwaiter();

            while (!processTask.IsCompleted)
                await Task.Delay(1).ConfigureAwait(false);

            runInfo.ExitCode = processTask.GetResult();
            runInfo.OutputStreamData = this.process.AllOutputLines.ToArray();
            runInfo.WasTerminated = this.process.WasTerminated;
            
            return runInfo;
        }
        
        protected override Task DoRun()
        {
            if (this.inputFilePath == null)
                throw new ArgumentNullException(nameof(this.inputFilePath));

            if (this.outputFilePath == null)
                throw new ArgumentNullException(nameof(this.outputFilePath));

            return Start(this.inputFilePath, this.outputFilePath);
        }

        protected async override Task DoTerminate()
        {
            if (this.process == null)
                return;

            await this.process.Terminate().ConfigureAwait(false);
        }
    }
}
