using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading;
using System.Windows;

using WpfApplicationLauncher.DataSourcing.FileSearchProvider;
using WpfApplicationLauncher.Logging;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace WpfApplicationLauncher
{
    internal static class Helpers
    {
        public static void Launch(ExecutableContext fa, IEnumerable<Uri> uris, bool waitForWindow)
        {
            List<Task> tasks = new List<Task>();

            foreach (Uri uri in uris)
            {
                if (fa == null)
                {
                    tasks.Add(Task.Factory.StartNew(() => Launch(new ExecutableContext() { Executable = uri.ToString() }, waitForWindow)));
                }
                else if (uri.IsFile)
                {
                    tasks.Add(Task.Factory.StartNew(() => Launch(fa.Format(Path.GetFileName(uri.LocalPath), Path.GetDirectoryName(uri.LocalPath), uri.ToString()), waitForWindow)));
                }
                else
                {
                    tasks.Add(Task.Factory.StartNew(() => Launch(fa.Format(String.Empty, String.Empty, uri.ToString()), waitForWindow)));
                }
            }

            Task.WaitAll(tasks.ToArray());
        }

        public static bool Launch(ExecutableContext fa, bool waitForWindow)
        {
            Process ps = new Process();
            ps.StartInfo.FileName = fa.Executable;
            ps.StartInfo.Arguments = fa.Arguments;
            ps.StartInfo.UseShellExecute = true;

            try
            {
                ps.Start();
            }
            catch (Exception ex)
            {
                Log.Trace(Log.EntrySeverity.Warning,
                    "Unable to launch " + String.Join(" ", fa.Executable, fa.Arguments)
                    + ".  Error: " + ex.Message);
                ps = null;
            }

            if (ps != null && waitForWindow)
            {
                try
                {
                    for (int i = 0; i < 20 && ps.MainWindowHandle == IntPtr.Zero; i++)
                    {
                        Thread.Sleep(200);
                    }
                }
                catch { }

                Thread.Sleep(200);
            }

            return ps != null;
        }

        public static bool IsModal(this Window w)
        {
            return (bool)w.Dispatcher.Invoke(new Func<object>(delegate()
            {
                return System.Windows.Interop.ComponentDispatcher.IsThreadModal as object;
            }));
        }
    }
}
