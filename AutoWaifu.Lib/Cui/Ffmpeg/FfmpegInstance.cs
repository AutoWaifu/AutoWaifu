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
    public class FfmpegInstance
    {
        ILogger Logger = Log.ForContext<FfmpegInstance>();

        public FfmpegInstance(string ffmpegPath)
        {
            FfmpegPath = ffmpegPath;
        }

        string FfmpegPath { get; }

        public struct RunInfo
        {
            public string Args;
            public string[] OutputStreamData;
            public int ExitCode;
            public bool WasTerminated;
        }

        public IFfmpegOptions Options;

        public async Task<RunInfo> Start(string inputImagePathFormat, string outputFilePath, Func<bool> shouldTerminateDelegate = null)
        {
            if (FfmpegPath == null || !File.Exists(FfmpegPath))
                Logger.Error("Cannot start ffmpeg for {@InputPathFormat} since the specified WaifuCaffe path is invalid! (Either not set or the file doesn't exist!)", inputImagePathFormat);

            if (Options == null)
                Logger.Error("Cannot start ffmpeg for {@InputPathFormat} when Options is null!", inputImagePathFormat);


            RunInfo runInfo = new RunInfo();

            var @params = Options.GetCuiParams(inputImagePathFormat, outputFilePath);

            runInfo.Args = @params;

            ProcessStartInfo psi = new ProcessStartInfo
            {
                Arguments = @params,
                CreateNoWindow = true,
                FileName = FfmpegPath,
                WorkingDirectory = Path.GetDirectoryName(FfmpegPath),
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                WindowStyle = ProcessWindowStyle.Hidden,
                UseShellExecute = false
            };

            List<string> outputLines = new List<string>();

            var process = new Process();
            process.StartInfo = psi;

            process.OutputDataReceived += (sender, data) => outputLines.Add("Info: " + data.Data);
            process.ErrorDataReceived += (sender, data) => outputLines.Add("Error: " + data.Data);

            process.Start();

            process.BeginOutputReadLine();
            process.BeginErrorReadLine();

            while (!process.WaitForExit(10))
            {
                if (shouldTerminateDelegate == null || !shouldTerminateDelegate())
                    await Task.Delay(10);
            }

            if (shouldTerminateDelegate == null || !shouldTerminateDelegate())
            {
                process.WaitForExit();
                runInfo.WasTerminated = false;
            }
            else
            {
                process.Kill();
                runInfo.WasTerminated = true;
            }

            runInfo.ExitCode = process.ExitCode;
            runInfo.OutputStreamData = outputLines.ToArray();

            return runInfo;
        }
    }
}
