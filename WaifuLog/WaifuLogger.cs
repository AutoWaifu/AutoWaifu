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



        public static Dictionary<LogMessageType, List<string>> LogHistory { get; } = new Dictionary<LogMessageType, List<string>>
        {
            { LogMessageType.Info, new List<string>() },
            { LogMessageType.ConfigWarning, new List<string>() },
            { LogMessageType.Warning, new List<string>() },
            { LogMessageType.LogicError, new List<string>() },
            { LogMessageType.ExternalError, new List<string>() },
            { LogMessageType.Exception, new List<string>() }
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
            object searchResult = Formatters.SingleOrDefault(kvp => kvp.Value.GetType().IsAssignableFrom(typeof(T))).Value;
            return searchResult as T;
        }


        public static void Info(string msg)
        {
            var formatter = GetFormatter<InfoLogFormatter>();
            string formatted = formatter.Format(msg);
            LogWritten?.Invoke(LogMessageType.Info, formatted);
            LogHistory[LogMessageType.Info].Add(formatted);
        }

        public static void Warning(string warning)
        {
            var formatter = GetFormatter<WarningLogFormatter>();
            var formatted = formatter.Format(warning);
            LogWritten?.Invoke(LogMessageType.Warning, formatted);
            LogHistory[LogMessageType.Warning].Add(formatted);
        }

        public static void LogicError(string error)
        {
            var formatter = GetFormatter<LogicErrorFormatter>();
            var formatted = formatter.Format(error);
            LogWritten?.Invoke(LogMessageType.LogicError, formatted);
            LogHistory[LogMessageType.LogicError].Add(formatted);
        }

        public static void ExternalError(string error)
        {
            var formatter = GetFormatter<ExternalErrorLogFormatter>();
            var formatted = formatter.Format(error);
            LogWritten?.Invoke(LogMessageType.ExternalError, formatted);
            LogHistory[LogMessageType.ExternalError].Add(formatted);
        }

        public static void ConfigWarning(string warning)
        {
            var formatter = GetFormatter<ConfigWarningLogFormatter>();
            var formatted = formatter.Format(warning);
            LogWritten?.Invoke(LogMessageType.ConfigWarning, formatted);
            LogHistory[LogMessageType.ConfigWarning].Add(formatted);
        }



        

        public static void Exception(Exception e)
        {
            Exception(null, e);
        }

        public static void Exception(string msg, Exception e)
        {
            var formatter = GetFormatter<ExceptionLogFormatter>();
            var formatted = formatter.Format(msg, e);
            LogWritten?.Invoke(LogMessageType.Exception, formatted);
            LogHistory[LogMessageType.Exception].Add(formatted);
        }





        public delegate void LogWrittenHandler(LogMessageType messageType, string logText);

        public static event LogWrittenHandler LogWritten;
    }
}
