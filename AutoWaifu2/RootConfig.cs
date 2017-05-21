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

        public static string EnabledFileTypeFilter = ".png|.jpeg|.jpg|.gif";

        public static Dispatcher AppDispatcher = null;
    }
}
