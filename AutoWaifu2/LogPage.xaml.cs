using Serilog.Events;
using System;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Media;

namespace AutoWaifu2
{
    /// <summary>
    /// Interaction logic for LogPage.xaml
    /// </summary>
    public partial class LogPage : Page
    {
        /// <summary>
        /// DO NOT USE THIS CONSTRUCTOR!
        /// </summary>
        public LogPage()
        {
            InitializeComponent();
        }

        DateTime _startTime = DateTime.Now;

        public void LogMessage(LogEventLevel level, string message)
        {
            Dispatcher.Invoke(() =>
            {
                var time = DateTime.Now - _startTime;

                //message = FileSystemHelper.AnonymizeFilePaths(message);
                message = message.Replace("\\\\", "\\");
                message = message.Replace("\\\"", "\"");

                Inline textRun = new Run(message) { Foreground = MessageColorMap[level] };

                if (level == LogEventLevel.Fatal)
                    textRun = new Bold(textRun);

                var para = new Paragraph(textRun);

                TextLogDocument.Blocks.Add(para);
                LogTextBox.ScrollToEnd();
            });
        }

        public Dictionary<LogEventLevel, Brush> MessageColorMap { get; set; } = new Dictionary<LogEventLevel, Brush>
        {
            { LogEventLevel.Information, new SolidColorBrush(Color.FromRgb(255,255,255)) },
            { LogEventLevel.Debug, new SolidColorBrush(Color.FromRgb(255,255,255)) },
            { LogEventLevel.Verbose, new SolidColorBrush(Color.FromRgb(255,255,255)) },
            { LogEventLevel.Warning, new SolidColorBrush(Color.FromRgb(255, 255, 0)) },
            { LogEventLevel.Error, new SolidColorBrush(Color.FromRgb(255, 0, 0)) },
            { LogEventLevel.Fatal, new SolidColorBrush(Color.FromRgb(255, 0, 0)) },
        };
    }
}
