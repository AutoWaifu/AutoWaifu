using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using WaifuLog.LogFormatters;

namespace WaifuLog
{
    public static class WaifuLogger
    {
        [Flags]
        public enum LogMessageType
        {
            Info            = 1 << 1,
            Warning         = 1 << 2,
            ConfigWarning   = 1 << 3,
            LogicError      = 1 << 4,
            ExternalError   = 1 << 5,
            Exception       = 1 << 6,

            All             = int.MaxValue
        }




        static Dictionary<LogMessageType, object> Formatters { get; } = new Dictionary<LogMessageType, object>
        {
            { LogMessageType.Info, new InfoLogFormatter() },
            { LogMessageType.Warning, new WarningLogFormatter() },
            { LogMessageType.ConfigWarning, new ConfigWarningLogFormatter() },
            { LogMessageType.LogicError, new LogicErrorFormatter() },
            { LogMessageType.ExternalError, new ExternalErrorLogFormatter() },
            { LogMessageType.Exception, new ExceptionLogFormatter() }
        };



        public static void SetFormatter<T>(T formatter) where T : class
        {
            if (formatter == null)
                throw new ArgumentNullException(nameof(formatter));

            var entry = Formatters.SingleOrDefault(kvp => kvp.Value.GetType().IsAssignableFrom(typeof(T)));
            if (entry.Value == null)
                throw new InvalidOperationException($"The specified {typeof(T).Name} does not inherit from any of the classes in WaifuLog.LogFormatters.");

            Formatters[entry.Key] = formatter;
        }

        public static T GetFormatter<T>() where T : class
        {
            return Formatters.SingleOrDefault(kvp => kvp.Value.GetType() == typeof(T)) as T;
        }


        public static void Info(string msg)
        {
            var formatter = GetFormatter<InfoLogFormatter>();
            LogWritten?.Invoke(LogMessageType.Info, formatter.Format(msg));
        }

        public static void Warning(string warning)
        {
            var formatter = GetFormatter<WarningLogFormatter>();
            LogWritten?.Invoke(LogMessageType.Warning, formatter.Format(warning));
        }

        public static void LogicError(string error)
        {
            var formatter = GetFormatter<LogicErrorFormatter>();
            LogWritten?.Invoke(LogMessageType.LogicError, formatter.Format(error));
        }

        public static void ExternalError(string error)
        {
            var formatter = GetFormatter<ExternalErrorLogFormatter>();
            LogWritten?.Invoke(LogMessageType.ExternalError, formatter.Format(error));
        }

        public static void ConfigWarning(string warning)
        {
            var formatter = GetFormatter<ConfigWarningLogFormatter>();
            LogWritten?.Invoke(LogMessageType.ConfigWarning, formatter.Format(warning));
        }



        

        public static void Exception(Exception e)
        {
            var formatter = GetFormatter<ExceptionLogFormatter>();
            LogWritten?.Invoke(LogMessageType.Exception, formatter.Format(null, e));
        }

        public static void Exception(string msg, Exception e)
        {
            var formatter = GetFormatter<ExceptionLogFormatter>();
            LogWritten?.Invoke(LogMessageType.Exception, formatter.Format(msg, e));
        }





        public delegate void LogWrittenHandler(LogMessageType messageType, string logText);

        public static event LogWrittenHandler LogWritten;
    }
}
