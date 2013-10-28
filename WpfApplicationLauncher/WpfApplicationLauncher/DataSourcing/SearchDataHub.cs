using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;
using System.Reflection;
using System.Collections;

using WpfApplicationLauncher.Logging;
using WpfApplicationLauncher.StaticData;

using SearchWords = System.Collections.Generic.HashSet<string>;

namespace WpfApplicationLauncher.DataSourcing
{
    /// <summary>
    /// Static application search provider.  Search Data Source API host.
    /// </summary>
    static class SearchDataHub
    {
        public delegate ISearchQuery QueryHubData(params String[] words);

        /// <summary>
        /// Search for classes satasfying an interface.
        /// </summary>
        /// <typeparam name="T">Interface to search for.</typeparam>
        /// <param name="folder">Folder to search in.</param>
        /// <returns>List of instantiations of classes satasfying supplied interface.</returns>
        private static List<T> GetPlugins<T>(string folder) where T : class
        {
            List<T> retVal = new List<T>();

            foreach (string file in Directory.GetFiles(folder, "*.dll"))
            {
                try
                {
                    Assembly assembly = Assembly.LoadFile(file);

                    foreach (Type type in assembly.GetTypes())
                    {
                        if (!type.IsClass || type.IsNotPublic) continue;

                        if (type.GetInterfaces().Contains(typeof(T)))
                        {
                            T obj = Activator.CreateInstance(type) as T;
                            if (obj != null)
                            {
                                retVal.Add(obj);
                            }
                        }
                    }
                }
                catch { }
            }

            return retVal;
        }

        /// <summary>
        /// Initialization; scan for plugins.
        /// </summary>
        static SearchDataHub()
        {
            DirectoryInfo exeDir = 
                new DirectoryInfo(Path.GetDirectoryName(
                    System.Reflection.Assembly.GetEntryAssembly().Location));

            DirectoryInfo pluginDir = new DirectoryInfo(Path.Combine(exeDir.FullName, "plugins"));

            if (pluginDir.Exists && Configuration.Instance.PluginsEnabled)
            {
                IEnumerable<ISearchProvider> plugins = GetPlugins<ISearchProvider>(pluginDir.FullName)
                    .AsParallel()
                    .Where(p =>
                    {
                        try
                        {
                            return p.Initialize();
                        }
                        catch (Exception ex)
                        {
                            Log.Trace(Log.EntrySeverity.Warning, "Unable to load plugin " + p.GetType() + ": " + ex.Message);
                            return false;
                        }
                    });
                AddProviders(plugins);
            }
        }

        private static readonly Dictionary<Type, ISearchProvider> providers = new Dictionary<Type, ISearchProvider>();

        public static void AddProvider(ISearchProvider provider, bool force = false)
        {
            AddProviders(new ISearchProvider[] { provider }, force);
        }

        public static void AddProviders(IEnumerable<ISearchProvider> input, bool force = false)
        {
            lock (providers)
            {
                foreach (ISearchProvider provider in input)
                {
                    if (!providers.ContainsKey(provider.GetType()))
                    {
                        providers.Add(provider.GetType(), provider);
                    }
                    else if (force)
                    {
                        providers[provider.GetType()] = provider;
                    }
                }
            }
        }

        /// <summary>
        /// Retrieve an existing provider by Type, if any.
        /// </summary>
        /// <typeparam name="T">Type of ISearchProvider to retrieve.</typeparam>
        /// <returns>Requested Type or null.</returns>
        public static T GetProvider<T>()  where T : class
        {
            Type t = typeof(T);
            if (providers.ContainsKey(t))
            {
                return providers[t] as T;
            }
            else
            {
                return null;
            }
        }

        /// <summary>
        /// Perform query on all ISearchProviders.
        /// </summary>
        /// <param name="words">Search words.</param>
        /// <returns>All ISearchResults from all ISearchProviders.</returns>
        public static IEnumerable<ISearchResult> Query(string searchQuery)
        {
            return Query(searchQuery.Split(' '));
        }

        /// <summary>
        /// Perform query on all ISearchProviders.
        /// </summary>
        /// <param name="words">Search words.</param>
        /// <returns>All ISearchResults from all ISearchProviders.</returns>
        public static IEnumerable<ISearchResult> Query(params string[] words)
        {
            return providers.SelectMany(t => t.Value.Query(words.Where(s => !String.IsNullOrWhiteSpace(s)).ToArray()).Results);
        }
    }
}
