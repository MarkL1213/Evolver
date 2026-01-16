using Avalonia.Threading;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;

namespace EvolverCore.ViewModels
{
    public class LogControlViewModel : ViewModelBase
    {
        private const int MaxLines = 20000; // Adjust as needed (20k is smooth even on low-end hardware)

        public ObservableCollection<string> LogLines { get; } = new ObservableCollection<string>();

        public LogControlViewModel()
        {
            // Public read-only wrapper is not needed unless binding from outside
        }

        /// <summary>
        /// Appends one or more lines to the log (thread-safe)
        /// </summary>
        public void AppendLines(string text)
        {
            string[] lines = text.Split(new[] { "\r\n", "\n" }, StringSplitOptions.RemoveEmptyEntries);

            Dispatcher.UIThread.InvokeAsync(() =>
            {
                foreach (var line in lines)
                {
                    LogLines.Add(line);

                    if (LogLines.Count > MaxLines)
                    {
                        LogLines.RemoveAt(0);
                    }
                }
            }, DispatcherPriority.Background);
        }

        /// <summary>
        /// Loads existing log file content (only the last N lines)
        /// </summary>
        public void LoadExistingLog(IEnumerable<string> lines)
        {
            Dispatcher.UIThread.InvokeAsync(() =>
            {
                LogLines.Clear();

                var recentLines = lines.TakeLast(MaxLines);
                foreach (var line in recentLines)
                {
                    LogLines.Add(line);
                }

                if (lines.Count() > MaxLines)
                {
                    LogLines.Insert(0, $"... (showing last {MaxLines} of {lines.Count()} lines) ...");
                }
            }, DispatcherPriority.Background);
        }
    }
}
