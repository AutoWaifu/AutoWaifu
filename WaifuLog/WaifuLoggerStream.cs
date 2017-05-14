using System;
using static WaifuLog.WaifuLogger;

namespace WaifuLog
{
    public class WaifuLoggerStream : IDisposable
    {
        public WaifuLoggerStream()
        {
            WaifuLogger.LogWritten += OnWaifuLogger_LogWritten;
        }

        public LogMessageType MessageFilter = LogMessageType.All;

        private void OnWaifuLogger_LogWritten(LogMessageType messageType, string logText)
        {
            if ((messageType & MessageFilter) == 0)
                LogWritten?.Invoke(messageType, logText);
        }

        public void Dispose()
        {
            WaifuLogger.LogWritten -= OnWaifuLogger_LogWritten;
        }

        public event LogWrittenHandler LogWritten;
    }
}
