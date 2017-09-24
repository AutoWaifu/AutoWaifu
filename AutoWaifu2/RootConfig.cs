using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Threading;

namespace AutoWaifu2
{
    static class RootConfig
    {
        public static string SettingsFilePath = "settings.json";



        public static string LogJsonFileLocation = "log.json";
        public static string LogTextFileLocation = "log.txt";


        public static string EnabledFileTypeFilter = ".png|.jpeg|.jpg|.gif";

        public static string CurrentVersion = "2.3.1";

#if DEBUG
        public static bool ForceNewConfig = false;
#else
        public static bool ForceNewConfig = false;
#endif

        public static Dispatcher AppDispatcher = null;

        public static bool IsHeadless = false;

        public static bool UseStatusServer = false;
    }
}
