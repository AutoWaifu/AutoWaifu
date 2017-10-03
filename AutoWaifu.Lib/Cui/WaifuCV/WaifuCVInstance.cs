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

namespace AutoWaifu.Lib.Cui.WaifuCV
{
    class WaifuCVInstance : Job
    {
        public WaifuCVInstance(string waifuCVPath)
        {
            WaifuCVPath = waifuCVPath;
        }

        public string WaifuCVPath { get; }

        public override ResourceConsumptionLevel ResourceConsumption => ResourceConsumptionLevel.High;

        public Waifu2xOptions Options;

        string inputFilePath, outputFilePath;
        ProcessWrapper process;

        public struct RunInfo
        {
            public string Args;
            public string[] OutputStreamData;
            public int ExitCode;
            public bool WasTerminated;
        }

        public RunInfo Result { get; private set; }

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

            paramParts.Add($"-i \"{inputFilePath}\"");

            paramParts.Add($"--scale_ratio {Options.GetOutputScale(inputFilePath)}");

            if (Options.NoiseLevel != Waifu2xOptions.ImageNoiseLevel.None)
            {
                paramParts.Add("--convert_mode denoise|scale");
                if (Options.NoiseLevel == Waifu2xOptions.ImageNoiseLevel.High)
                    paramParts.Add("--noise_level 2");
                else
                    paramParts.Add("--noise_level 1");
            }
            
            paramParts.Add($"-o \"{outputFilePath}\"");

            return string.Join(" ", paramParts);
        }

        public void Configure(string inputFilePath, string outputFilePath)
        {
            this.inputFilePath = inputFilePath;
            this.outputFilePath = outputFilePath;
        }


        async Task Start(string inputFilePath, string outputFilePath)
        {
            if (WaifuCVPath == null || !File.Exists(WaifuCVPath))
            {
                Logger.Error("Cannot start WaifuCV for {@InputPath} since the specified WaifuCV path is invalid! (Either not set or the file doesn't exist!)", inputFilePath);
                Result = new RunInfo { ExitCode = -1 };
                return;
            }

            if (Options == null)
            {
                Logger.Error("Cannot start WaifuCV for {@InputPath} when Options is null!", inputFilePath);
                Result = new RunInfo { ExitCode = -1 };
                return;
            }

            if (this.process != null)
            {
                Logger.Warning("Cannot start WaifuCVInstance after it's already started");
                Result = new RunInfo { ExitCode = -1 };
                return;
            }

            var runInfo = new RunInfo();

            var @params = GetCuiParams(inputFilePath, outputFilePath);

            Logger.Debug("Running waifucv with params: {Waifu2xCaffeParams}", @params);
            runInfo.Args = @params;

            this.process = new ProcessWrapper
            {
                ProgramPath = WaifuCVPath,
                CommandlineArgs = @params
            };

            runInfo.ExitCode = await this.process.Start().ConfigureAwait(false);

            runInfo.OutputStreamData = this.process.AllOutputLines.ToArray();
            runInfo.WasTerminated = this.process.WasTerminated;

            Result = runInfo;

            State = JobState.Completed;
        }

        protected override async Task DoTerminate()
        {
            if (this.process == null)
                return;

            await this.process.Terminate().ConfigureAwait(false);
        }

        protected override Task DoRun()
        {
            if (this.inputFilePath == null)
                throw new ArgumentNullException($"Can't run WaifuCVInstance since {this.inputFilePath} is null");

            if (this.outputFilePath == null)
                throw new ArgumentNullException($"Can't run WaifuCVInstance since {this.outputFilePath} is null");

            return Start(this.inputFilePath, this.outputFilePath);
        }
    }
}
