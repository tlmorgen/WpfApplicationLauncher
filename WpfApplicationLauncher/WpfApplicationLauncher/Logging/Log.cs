using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using WpfApplicationLauncher.GUI;

namespace WpfApplicationLauncher.Logging
{
    public static class Log
    {
        public enum EntrySeverity
        {
            Info,
            Warning,
            Error,
            FatalError
        }

        public class Entry
        {
            public EntrySeverity Severity { get; private set; }
            public String Message { get; private set; }

            internal Entry(EntrySeverity severity, string message)
            {
                Severity = severity;
                Message = message;
            }
        }

        private static readonly object mutex = new object();
        private static readonly LinkedList<Entry> logRoll = new LinkedList<Entry>();
        private static LogViewer viewer;

        public static IEnumerable<Entry> LogRoll
        {
            get
            {
                return new LinkedList<Entry>(logRoll);
            }
        }

        public static void Trace(EntrySeverity severity, string message)
        {
            lock (mutex)
            {
                logRoll.AddLast(new Entry(severity, message));
            }
            if ((int)severity > (int)EntrySeverity.Info)
            {
                ShowLog();
            }
        }

        public static void ShowLog()
        {
            if (viewer == null)
            {
                viewer = new LogViewer();
                viewer.ShowDialog();
                viewer = null;
            }
            else
            {
                viewer.Activate();
            }
        }
    }
}
