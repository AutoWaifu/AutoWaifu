using AutoWaifu.Lib.Jobs;
using Serilog;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AutoWaifu.Lib.Cui.Ffmpeg
{
    public class FfmpegInstance : Job
    {
        public FfmpegInstance(string ffmpegPath)
        {
            FfmpegPath = ffmpegPath;
        }

        string FfmpegPath { get; }
        string inputImagePathFormat, outputFilePath;

        public void Configure(string inputImagePathFormat, string outputFilePath)
        {
            this.inputImagePathFormat = inputImagePathFormat;
            this.outputFilePath = outputFilePath;
        }

        public override ResourceConsumptionLevel ResourceConsumption => ResourceConsumptionLevel.Medium;

        public struct RunInfo
        {
            public string Args;
            public string[] OutputStreamData;
            public int ExitCode;
            public bool WasTerminated;
        }

        public RunInfo Result { get; private set; }

        public IFfmpegOptions Options;

        ProcessWrapper process;

        void RegisterLine(List<string> allLines, string line)
        {
            lock (allLines)
            {
                allLines.Add(line);
            }
        }

        async Task Start(string inputImagePathFormat, string outputFilePath)
        {
            Logger.Verbose("Trace");

            if (FfmpegPath == null || !File.Exists(FfmpegPath))
                throw new Exception($"Cannot start ffmpeg for {inputImagePathFormat} since the specified WaifuCaffe path is invalid! (Either not set or the file doesn't exist!)");

            if (Options == null)
                throw new Exception($"Cannot start ffmpeg for {inputImagePathFormat} when Options is null!");

            if (this.process != null)
                throw new Exception("This ffmpeg instance is already running!");


            RunInfo runInfo = new RunInfo();

            var @params = Options.GetCuiParams(inputImagePathFormat, outputFilePath);

            Logger.Debug("Running ffmpeg with params: {FfmpegParams}", @params);

            runInfo.Args = @params;

            this.process = new ProcessWrapper
            {
                CommandlineArgs = @params,
                ProgramPath = FfmpegPath
            };
            
            var processTask = this.process.Start();

            Logger.Verbose("Started ffmpeg, waiting for exit");
            
            while (!processTask.Wait(1))
                await Task.Delay(10).ConfigureAwait(false);

            Logger.Verbose("ffmpeg stopped, exit code was {FfmpegExitCode}", processTask.Result);

            runInfo.ExitCode = processTask.Result;
            runInfo.OutputStreamData = this.process.AllOutputLines.ToArray();

            Result = runInfo;
            State = JobState.Completed;
        }
        
        

        protected override Task DoRun()
        {
            if (!(this.Options is FfmpegRawOptions))
            {
                if (this.inputImagePathFormat == null)
                    throw new ArgumentNullException(nameof(this.inputImagePathFormat));

                if (this.outputFilePath == null)
                    throw new ArgumentNullException(nameof(this.outputFilePath));
            }

            return this.Start(this.inputImagePathFormat, this.outputFilePath);
        }

        protected override async Task DoTerminate()
        {
            Logger.Verbose("Trace");

            if (this.process == null)
                return;

            await this.process.Terminate().ConfigureAwait(false);
        }
    }
}
