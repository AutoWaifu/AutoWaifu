using System;
using System.Collections.Generic;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Media;
using WaifuLog;

namespace AutoWaifu2
{
    /// <summary>
    /// Interaction logic for LogPage.xaml
    /// </summary>
    public partial class LogPage : Page
    {
        /// <summary>
        /// DO NOT USE!
        /// </summary>
        public LogPage()
        {
            InitializeComponent();
        }

        DateTime _startTime = DateTime.Now;

        WaifuLoggerStream _logStream;
        public WaifuLoggerStream LogStream
        {
            get => _logStream;

            set
            {
                if (_logStream != null)
                    _logStream.LogWritten -= OnLogStream_LogWritten;

                _logStream = value;

                _logStream.LogWritten += OnLogStream_LogWritten;
            }
        }

        public Dictionary<WaifuLogger.LogMessageType, Brush> MessageColorMap { get; set; } = new Dictionary<WaifuLogger.LogMessageType, Brush>
        {
            { WaifuLogger.LogMessageType.Info, new SolidColorBrush(Color.FromRgb(255,255,255)) },
            { WaifuLogger.LogMessageType.Warning, new SolidColorBrush(Color.FromRgb(255, 255, 0)) },
            { WaifuLogger.LogMessageType.ConfigWarning, new SolidColorBrush(Color.FromRgb(255, 255, 0)) },
            { WaifuLogger.LogMessageType.Exception, new SolidColorBrush(Color.FromRgb(255, 0, 0)) },
            { WaifuLogger.LogMessageType.ExternalError, new SolidColorBrush(Color.FromRgb(255, 0, 0)) },
            { WaifuLogger.LogMessageType.LogicError, new SolidColorBrush(Color.FromRgb(255, 0, 0)) }
        };

        private void OnLogStream_LogWritten(WaifuLogger.LogMessageType messageType, string logText)
        {
            Dispatcher.Invoke(() =>
            {
                var time = DateTime.Now - _startTime;

                var para = new Paragraph(new Run($"{time.ToString("c")} {messageType}: {logText}") { Foreground = MessageColorMap[messageType] });

                TextLogDocument.Blocks.Add(para);
            });
        }
    }
}
