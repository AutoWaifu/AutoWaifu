using Serilog;
using Serilog.Events;
using Serilog.Formatting.Json;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.ExceptionServices;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Threading;

namespace AutoWaifu2
{
    /// <summary>
    /// Interaction logic for App.xaml
    /// </summary>
    public partial class App : Application
    {
        public ILogger Logger;

        class LogObserver : IObserver<LogEvent>
        {
            public void OnCompleted()
            {

            }

            public void OnError(Exception error)
            {

            }

            public void OnNext(LogEvent value)
            {
                string message = value.RenderMessage();

                string formatted = value.Level + ": " + message;

                NewMessageEvent?.Invoke(value.Level, formatted);
            }

            event Action<LogEventLevel, string> NewMessageEvent;

            public LogObserver OnMessage(Action<LogEventLevel, string> msgCallback)
            {
                NewMessageEvent += msgCallback;
                return this;
            }
        }

        public App()
        {
            FileExt.TryDelete(RootConfig.LogTextFileLocation);
            FileExt.TryDelete(RootConfig.LogJsonFileLocation);

            Serilog.Debugging.SelfLog.Enable((msg) =>
            {
                Debug.WriteLine("Serilog says: " + msg);
            });



            string logOutputTemplate = "{Timestamp:HH:mm:ss} [{Level} {CallerMethodName}] {Message}{NewLine}{Exception}";

            Log.Logger = new LoggerConfiguration()
                            .MinimumLevel.Verbose()
                            .Enrich.FromLogContext()
                            //.Enrich.With(new SerilogBreakOnLogEnricher(LogEventLevel.Error))
                            .Enrich.With(new SerilogCallingMethodEnricher())
                            .WriteTo.File(RootConfig.LogTextFileLocation, outputTemplate: logOutputTemplate)
                            .WriteTo.File(new JsonFormatter(null, true), RootConfig.LogJsonFileLocation)
                            .WriteTo.Observers(events => events.Subscribe(new LogObserver().OnMessage((a, b) => Logged?.Invoke(a, b))))
                            .CreateLogger();

            Logger = Log.ForContext<App>();

            DispatcherUnhandledException += App_DispatcherUnhandledException;

            AppDomain.CurrentDomain.UnhandledException += CurrentDomain_UnhandledException;
            AppDomain.CurrentDomain.FirstChanceException += CurrentDomain_FirstChanceException;

            var cmd = Environment.CommandLine.Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);

            if (cmd.Any(c => c.ToLower() == "-debug"))
                Debugger.Launch();
        }

        public static event Action<LogEventLevel, string> Logged;

        public static bool IsClosing = false;

        

        private void App_DispatcherUnhandledException(object sender, DispatcherUnhandledExceptionEventArgs e)
        {
            if (IsClosing)
                return;

            e.Handled = true;

            Logger.Error(e.Exception, "An unhandled exception occurred within the Dispatcher");
            Log.CloseAndFlush();
        }

        private void CurrentDomain_FirstChanceException(object sender, FirstChanceExceptionEventArgs e)
        {
            if (IsClosing)
                return;

            var stackTraceString = e.Exception.StackTrace;
            var relevantLine = stackTraceString.Split('\n')[0];

            FileSystemHelper.AnonymizeFilePaths(stackTraceString);

            var stackTraceProperty = typeof(Exception).GetProperty("StackTrace", System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Instance);
            //stackTraceProperty.SetValue()

            Logger.Error("An exception occurred: {@Exception}", e);
        }

        private void CurrentDomain_UnhandledException(object sender, UnhandledExceptionEventArgs e)
        {
            if (IsClosing)
                return;

            Logger.Error("An unhandled exception occurred: {@Exception}", e.ExceptionObject as Exception);
            Log.CloseAndFlush();
        }
    }
}
