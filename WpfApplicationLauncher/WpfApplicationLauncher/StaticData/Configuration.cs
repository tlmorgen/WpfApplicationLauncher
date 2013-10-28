using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Text;
using System.Xml.Serialization;

using Keys = System.Windows.Forms.Keys;

using WpfApplicationLauncher.Logging;
using WpfApplicationLauncher.GUI;
using WpfApplicationLauncher.DataSourcing.FileSearchProvider;

namespace WpfApplicationLauncher.StaticData
{
    public class Configuration
    {
        #region Statics

        public delegate void ConfigurationChangeHandler(Configuration oldConfig, Configuration newConfig);

        public static event ConfigurationChangeHandler Changed;

        private static string configPath = Path.ChangeExtension(System.Reflection.Assembly.GetEntryAssembly().Location, "xml");
        private static ConfigurationDialog configWindow;

        private static bool loading = false;

        private static readonly object mutex = new object();
        private static Configuration instance;
        public static Configuration Instance
        {
            get
            {
                if (instance == null)
                {
                    lock (mutex)
                    {
                        instance = new Configuration();
                    }
                }
                return new Configuration(instance);
            }
            set
            {
                Configuration oldConfig = instance;

                lock (mutex)
                {
                    instance = value;
                }

                if (!loading)
                {
                    Save();
                }

                if (Changed != null)
                {
                    Changed(oldConfig, Instance);
                }
            }
        }

        static Configuration()
        {
            configWindow = null;

            if (File.Exists(configPath))
            {
                string backupPath = Path.Combine(Path.GetDirectoryName(configPath), Path.GetRandomFileName());

                try
                {
                    Load();
                }
                catch (Exception)
                {
                    File.Move(configPath, backupPath);
                    Log.Trace(Log.EntrySeverity.Warning, 
                        "There was an error loading the configuration file." + Environment.NewLine +
                            "Backing up your configuration to " + backupPath + Environment.NewLine +
                            "Loading with empty configuration");
                }
            }
        }

        private static void Save()
        {
            TextWriter tout = new StreamWriter(new FileStream(configPath, FileMode.Create, FileAccess.Write), Encoding.ASCII);
            try
            {
                XmlSerializer xmlSer = new XmlSerializer(Instance.GetType());
                xmlSer.Serialize(tout, Instance);
            }
            finally
            {
                tout.Close();
            }
        }

        private static void Load()
        {
            loading = true;
            TextReader tin = new StreamReader(configPath);
            try
            {
                XmlSerializer xmlSer = new XmlSerializer(Instance.GetType());
                lock (mutex)
                {
                    Instance = (Configuration)xmlSer.Deserialize(tin);
                }
            }
            finally
            {
                tin.Close();
                loading = false;
            }
        }

        public static void ShowConfigWindow()
        {
            if (configWindow == null)
            {
                configWindow = new ConfigurationDialog();
                configWindow.ShowDialog();
                configWindow = null;
            }
            else
            {
                configWindow.Activate();
            }
        }

        #endregion

        #region Variables / Settings

        public ObservableCollection<String> SearchPaths = new ObservableCollection<String>();
        public ObservableCollection<String> SearchExtensions = new ObservableCollection<String>();
        public ObservableCollection<ExecutableContext> FileActions = new ObservableCollection<ExecutableContext>();
        public Keys NewSearchHotKey = Keys.None;
        public Keys LastSearchHotKey = Keys.None;
        public Boolean PluginsEnabled = false;

        #endregion

        #region Ctor

        private Configuration()
        {
        }

        public Configuration(Configuration copyFrom)
        {
            if (copyFrom != null)
            {
                SearchPaths = new ObservableCollection<String>(copyFrom.SearchPaths);
                SearchExtensions = new ObservableCollection<String>(copyFrom.SearchExtensions);
                FileActions = new ObservableCollection<ExecutableContext>(copyFrom.FileActions);
                NewSearchHotKey = copyFrom.NewSearchHotKey;
                LastSearchHotKey = copyFrom.LastSearchHotKey;
                PluginsEnabled = copyFrom.PluginsEnabled;
            }
        }

        #endregion

    }
}
