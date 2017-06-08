using Serilog;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AutoWaifu.Lib
{
    public class Loggable
    {
        internal Loggable()
        {
            Logger = Log.ForContext(this.GetType());
        }

        protected ILogger Logger { get; }
    }
}
