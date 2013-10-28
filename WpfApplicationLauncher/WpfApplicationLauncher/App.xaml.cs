using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data;
using System.Linq;
using System.Windows;

using Keys = System.Windows.Forms.Keys;

using WpfApplicationLauncher.DataSourcing;
using WpfApplicationLauncher.DataSourcing.FileSearchProvider;
using WpfApplicationLauncher.GUI;
using WpfApplicationLauncher.Logging;
using WpfApplicationLauncher.ManagedWinapi;
using WpfApplicationLauncher.Properties;
using WpfApplicationLauncher.StaticData;

namespace WpfApplicationLauncher
{
    /// <summary>
    /// Interaction logic for App.xaml
    /// </summary>
    public partial class App : Application
    {
        #region Static 

        #region Variables

        private static ManagedWinapi.Hotkey hkNew;
        private static ManagedWinapi.Hotkey hkLast;
        private static SearchWindow mainWindow;
        private static SearchWindow.SearchWindowAshes lastSearch;

        #endregion Variables

        #region Ctor

        static App()
        {
            Configuration.Changed += Configuration_Changed;
        }

        #endregion Ctor

        #region Event Handlers

        static void Configuration_Changed(Configuration oldConfig, Configuration newConfig)
        {
            LoadFileDBConfigAndScan();

            try
            {
                UpdateHotKeys();
            }
            catch (HotkeyAlreadyInUseException ex)
            {
                Log.Trace(Log.EntrySeverity.Error, "Unable to load hotkeys. Error: " + ex.Message);
                Configuration cfg = Configuration.Instance;
                cfg.NewSearchHotKey = Keys.None;
                cfg.LastSearchHotKey = Keys.None;
                Configuration.Instance = cfg;
            }
        }

        static void hkLast_HotkeyPressed(object sender, EventArgs e)
        {
            ShowSearchWindow(false);
        }

        static void hkNew_HotkeyPressed(object sender, EventArgs e)
        {
            ShowSearchWindow();
        }

        #endregion Event Handlers

        #region Methods

        private static void UpdateHotKeys()
        {
            if (hkNew != null)
                hkNew.Enabled = false;
            if (hkLast != null)
                hkLast.Enabled = false;

            Configuration cfg = Configuration.Instance;
            if ((cfg.NewSearchHotKey & Keys.KeyCode) != Keys.None)
            {
                hkNew = new Hotkey();

                hkNew.Alt = cfg.NewSearchHotKey.HasFlag(Keys.Alt);
                hkNew.Ctrl = cfg.NewSearchHotKey.HasFlag(Keys.Control);
                hkNew.Shift = cfg.NewSearchHotKey.HasFlag(Keys.Shift);
                hkNew.WindowsKey = cfg.NewSearchHotKey.HasFlag(Keys.LWin) || cfg.NewSearchHotKey.HasFlag(Keys.RWin);
                hkNew.KeyCode = cfg.NewSearchHotKey & Keys.KeyCode;

                hkNew.HotkeyPressed += new EventHandler(hkNew_HotkeyPressed);

                hkNew.Enabled = true;
            }

            if ((cfg.LastSearchHotKey & Keys.KeyCode) != Keys.None)
            {

                hkLast = new Hotkey();

                hkLast.Alt = cfg.LastSearchHotKey.HasFlag(Keys.Alt);
                hkLast.Ctrl = cfg.LastSearchHotKey.HasFlag(Keys.Control);
                hkLast.Shift = cfg.LastSearchHotKey.HasFlag(Keys.Shift);
                hkLast.WindowsKey = cfg.LastSearchHotKey.HasFlag(Keys.LWin) || cfg.LastSearchHotKey.HasFlag(Keys.RWin);
                hkLast.KeyCode = cfg.LastSearchHotKey & Keys.KeyCode;

                hkLast.HotkeyPressed += new EventHandler(hkLast_HotkeyPressed);

                hkLast.Enabled = true;
            }
        }

        public static void LoadFileDBConfigAndScan()
        {
            FileDB fileDB = FileDB.CreateFileDB(Configuration.Instance);
            SearchDataHub.AddProvider(fileDB, true);
        }

        private static void ShowSearchWindow(bool clear = true)
        {
            if (mainWindow != null &&
                mainWindow.IsModal())
            {
                mainWindow.ClearSearch();
                mainWindow.Activate();
                return;
            }

            if (clear)
            {
                mainWindow = new SearchWindow();
            }
            else
            {
                mainWindow = new SearchWindow(lastSearch);
            }

            try
            {
                mainWindow.ShowDialog();
                mainWindow.Close();
                lastSearch = new SearchWindow.SearchWindowAshes(mainWindow);
                mainWindow = null;
            }
            catch (Exception ex)
            {
                Log.Trace(Log.EntrySeverity.Error, "Search Window Error: " + ex.Message);
            }
        }

        #endregion Methods

        #endregion Static

        #region Event Handlers

        private void Application_Startup(object sender, StartupEventArgs e)
        {
            try
            {
                UpdateHotKeys();
                NotifyIcon.Initialize();
                LoadFileDBConfigAndScan();
                ShowSearchWindow();
            }
            catch (Exception ex)
            {
                try
                {
                    Log.Trace(Log.EntrySeverity.FatalError, ex.Message);
                } catch { }
            }
        }

        #endregion Event Handlers
    }
}
