using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;

namespace AutoWaifu
{
    /// <summary>
    /// Interaction logic for App.xaml
    /// </summary>
    public partial class App : Application
    {
        public App()
        {
            AppDomain.CurrentDomain.UnhandledException += CurrentDomain_UnhandledException;
        }

        private void CurrentDomain_UnhandledException(object sender, UnhandledExceptionEventArgs e)
        {
            Logger.Exception(e.ExceptionObject as Exception);

            MessageBox.Show("An error occurred! Info can be found in log.txt, send to /u/yiffanon if you can!");
#if DEBUG
            Debugger.Break();
#endif
        }
    }
}
