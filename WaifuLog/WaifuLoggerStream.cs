using System;
using System.Linq;
using static WaifuLog.WaifuLogger;

namespace WaifuLog
{
    public class WaifuLoggerStream : IDisposable
    {
        public WaifuLoggerStream()
        {
            WaifuLogger.LogWritten += OnWaifuLogger_LogWritten;
        }

        public WaifuLoggerStream(bool signalPreExistingLogs, LogMessageType messageFilter) : this()
        {
            MessageFilter = messageFilter;

            if (signalPreExistingLogs)
            {
                foreach (LogMessageType msgType in Enum.GetValues(typeof(LogMessageType)))
                {
                    if (MessageFilter.HasFlag(msgType))
                    {
                        foreach (var log in WaifuLogger.LogHistory[msgType])
                            LogWritten?.Invoke(msgType, log);
                    }
                }
            }
        }


        public LogMessageType MessageFilter = LogMessageType.All;

        private void OnWaifuLogger_LogWritten(LogMessageType messageType, string logText)
        {
            if ((messageType & MessageFilter) != 0)
                LogWritten?.Invoke(messageType, logText);
        }

        public void Dispose()
        {
            WaifuLogger.LogWritten -= OnWaifuLogger_LogWritten;
        }

        public event LogWrittenHandler LogWritten;
    }
}
