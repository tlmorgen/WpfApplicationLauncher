using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using BaseNotifyIcon = System.Windows.Forms.NotifyIcon;

using WpfApplicationLauncher.Logging;
using WpfApplicationLauncher.Properties;
using WpfApplicationLauncher.StaticData;

namespace WpfApplicationLauncher.GUI
{
    public static class NotifyIcon
    {
        private static BaseNotifyIcon instance;

        static NotifyIcon()
        {
            App.Current.Exit += Application_Exit;
        }

        public static void Initialize()
        {
            if (instance != null)
            {
                instance.Visible = false;
            }

            instance = new BaseNotifyIcon();
            instance.Icon = System.Drawing.Icon.FromHandle(WpfApplicationLauncher.Properties.Resources.lightning.GetHicon());
            instance.Text = Settings.Default.Name;
            instance.Visible = true;
            instance.ContextMenu = new System.Windows.Forms.ContextMenu();
            instance.ContextMenu.MenuItems.Add("&Configuration", (s, e) => Configuration.ShowConfigWindow());
            instance.ContextMenu.MenuItems.Add("&Log", (s, e) => Log.ShowLog());
            instance.ContextMenu.MenuItems.Add("-");
            instance.ContextMenu.MenuItems.Add("&Exit", (s, e) => App.Current.Shutdown());

        }

        private static void Application_Exit(object sender, System.Windows.ExitEventArgs e)
        {
            if (instance != null)
            {
                instance.Visible = false;
            }
        }
    }
}
