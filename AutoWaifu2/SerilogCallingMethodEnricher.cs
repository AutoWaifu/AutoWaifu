using Serilog.Core;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Serilog.Events;
using System.Text.RegularExpressions;

namespace AutoWaifu2
{
    class SerilogCallingMethodEnricher : ILogEventEnricher
    {
        public void Enrich(LogEvent logEvent, ILogEventPropertyFactory propertyFactory)
        {
            string stackTrace = Environment.StackTrace;

            string[] traceLines = stackTrace.Split(Environment.NewLine.ToCharArray(), StringSplitOptions.RemoveEmptyEntries);

            int serilogTraceEndLine = Array.IndexOf(traceLines, traceLines.Last(l => l.Contains("Serilog.")));
            string callerLine = traceLines[serilogTraceEndLine + 1];

            var callerMatch = Regex.Match(callerLine, @"^\s+at (.*) in");
            if (callerMatch.Success)
            {
                string callerMethod = callerMatch.Groups[1].Value;
                logEvent.AddOrUpdateProperty(propertyFactory.CreateProperty("CallerMethodName", callerMethod));
            }
        }
    }
}
