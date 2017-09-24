using Serilog.Core;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Serilog.Events;
using System.Diagnostics;

namespace AutoWaifu2
{
    class SerilogBreakOnLogEnricher : ILogEventEnricher
    {
        public SerilogBreakOnLogEnricher(LogEventLevel filter)
        {
            this.filter = filter;
        }

        LogEventLevel filter;

        public void Enrich(LogEvent logEvent, ILogEventPropertyFactory propertyFactory)
        {
            if (logEvent.Level >= this.filter && Debugger.IsAttached)
            {
                Debugger.Break();
            }
        }
    }
}
