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
    class WaifuCaffeInstance : Loggable
    {
        public WaifuCaffeInstance(string waifuCaffePath)
        {
            WaifuCaffePath = waifuCaffePath;
        }

        public string WaifuCaffePath { get; }

        public WaifuCaffeOptions Options;

        public struct RunInfo
        {
            public string Args;
            public string[] OutputStreamData;
            public int ExitCode;
            public bool WasTerminated;
        }

        void RegisterLine(List<string> allLines, string line)
        {
            lock (allLines)
            {
                allLines.Add(line);
            }
        }

        public async Task<RunInfo> Start(string inputFilePath, string outputFilePath, Func<bool> shouldTerminateDelegate = null)
        {
            if (WaifuCaffePath == null || !File.Exists(WaifuCaffePath))
                Logger.Error("Cannot start WaifuCaffe for {@InputPath} since the specified WaifuCaffe path is invalid! (Either not set or the file doesn't exist!)", inputFilePath);

            if (Options == null)
                Logger.Error("Cannot start WaifuCaffe for {@InputPath} when Options is null!", inputFilePath);

            var runInfo = new RunInfo();

            var @params = Options.GetCuiParams(inputFilePath, outputFilePath);

            runInfo.Args = @params;

            ProcessStartInfo psi = new ProcessStartInfo
            {
                Arguments = @params,
                CreateNoWindow = true,
                FileName = WaifuCaffePath,
                WorkingDirectory = Path.GetDirectoryName(WaifuCaffePath),
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                WindowStyle = ProcessWindowStyle.Hidden,
                UseShellExecute = false
            };

            List<string> outputLines = new List<string>();

            var process = new Process();
            process.StartInfo = psi;

            process.OutputDataReceived += (sender, data) => RegisterLine(outputLines, "Info: " + data.Data);
            process.ErrorDataReceived += (sender, data) => RegisterLine(outputLines, "Error: " + data.Data);

            process.Start();

            process.BeginOutputReadLine();
            process.BeginErrorReadLine();

            while (!process.WaitForExit(10))
            {
                if (shouldTerminateDelegate == null || !shouldTerminateDelegate())
                    await Task.Delay(10);
                else
                    break;
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
