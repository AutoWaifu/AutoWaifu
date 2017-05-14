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

        WaifuLoggerStream _logStream;
        public WaifuLoggerStream LogStream
        {
            get => _logStream;

            set
            {
                if (_logStream != null)
                    _logStream.LogWritten -= OnLogStream_LogWritten;

                _logStream.LogWritten += OnLogStream_LogWritten;
            }
        }

        public Dictionary<WaifuLogger.LogMessageType, Brush> MessageColorMap { get; set; }

        private void OnLogStream_LogWritten(WaifuLogger.LogMessageType messageType, string logText)
        {
            var para = new Paragraph(new Run($"{messageType}: {logText}") { Foreground=MessageColorMap[messageType] });

            TextLogDocument.Blocks.Add(para);
        }
    }
}
