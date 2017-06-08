using Serilog;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data;
using System.Linq;
using System.Runtime.ExceptionServices;
using System.Threading.Tasks;
using System.Windows;

namespace AutoWaifu2
{
    /// <summary>
    /// Interaction logic for App.xaml
    /// </summary>
    public partial class App : Application
    {
        ILogger Logger = Log.ForContext<App>();

        public App()
        {
            AppDomain.CurrentDomain.UnhandledException += CurrentDomain_UnhandledException;
            AppDomain.CurrentDomain.FirstChanceException += CurrentDomain_FirstChanceException;
        }

        private void CurrentDomain_FirstChanceException(object sender, FirstChanceExceptionEventArgs e)
        {
            var stackTraceString = e.Exception.StackTrace;
            var relevantLine = stackTraceString.Split('\n')[0];

            FileSystemHelper.AnonymizeFilePaths(stackTraceString);

            var stackTraceProperty = typeof(Exception).GetProperty("StackTrace", System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Instance);
            //stackTraceProperty.SetValue()

            //Logger.Error(e.Exception, "An exception occurred");
        }

        private void CurrentDomain_UnhandledException(object sender, UnhandledExceptionEventArgs e)
        {
            Logger.Error(e.ExceptionObject as Exception, "An unhandled exception occurred");
        }
    }
}
