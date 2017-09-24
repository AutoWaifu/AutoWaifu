using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AutoWaifu2
{
    class ProcessHelper
    {
        public static void Terminate(string exeName)
        {
            var processes = Process.GetProcessesByName(exeName);
            foreach (var p in processes)
                p.Kill();
        }
    }
}
