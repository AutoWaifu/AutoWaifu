using Serilog;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AutoWaifu.Lib.Cui
{
    class ProcessWrapper
    {
        static ILogger Logger = Log.ForContext<ProcessWrapper>();

        public ProcessWrapper()
        {
            ReceivedOutput += (line) =>
            {
                OutputLines.Enqueue(line);
                AllOutputLines.Enqueue(line);
            };

            ReceivedError += (line) =>
            {
                ErrorLines.Enqueue(line);
                AllOutputLines.Enqueue(line);
            };
        }

        public string CommandlineArgs { get; set; } = null;
        public bool HideWindow { get; set; } = true;
        public string ProgramPath { get; set; } = null;

        public event Action<string> ReceivedOutput;
        public event Action<string> ReceivedError;

        public ConcurrentQueue<string> OutputLines { get; private set; }
        public ConcurrentQueue<string> ErrorLines { get; private set; }

        public ConcurrentQueue<string> AllOutputLines { get; private set; }

        public bool WasTerminated { get; private set; }

        Process runningProcess = null;

        public async Task<int> Start()
        {
            if (this.runningProcess != null)
                throw new InvalidOperationException("This process wrapper is already running!");

            if (ProgramPath == null)
                throw new InvalidOperationException("No program was assigned when trying to start");

            Logger.Verbose("Trace");

            WasTerminated = false;

            OutputLines = new ConcurrentQueue<string>();
            ErrorLines = new ConcurrentQueue<string>();
            AllOutputLines = new ConcurrentQueue<string>();

            Process process = new Process();
            process.EnableRaisingEvents = true;
            process.StartInfo = new ProcessStartInfo
            {
                Arguments = this.CommandlineArgs,
                CreateNoWindow = HideWindow,
                FileName = ProgramPath,
                RedirectStandardError = true,
                RedirectStandardOutput = true,
                UseShellExecute = false,
                WindowStyle = ProcessWindowStyle.Hidden,
                WorkingDirectory = Path.GetDirectoryName(ProgramPath)
            };

            process.OutputDataReceived += (sender, data) => ReceivedOutput?.Invoke(data.Data);
            process.ErrorDataReceived += (sender, data) => ReceivedError?.Invoke(data.Data);

            process.Start();

            this.runningProcess = process;

            process.BeginOutputReadLine();
            process.BeginErrorReadLine();

            while (!process.WaitForExit(1))
                await Task.Delay(1).ConfigureAwait(false);

            this.runningProcess = null;

            return process.ExitCode;
        }

        public async Task Terminate()
        {
            Logger.Verbose("Trace");

            if (this.runningProcess == null)
                throw new InvalidOperationException("Cannot terminate a process that has not been started");

            WasTerminated = true;

            this.runningProcess.Kill();

            while (this.runningProcess != null)
                await Task.Delay(1).ConfigureAwait(false);
        }
    }
}
