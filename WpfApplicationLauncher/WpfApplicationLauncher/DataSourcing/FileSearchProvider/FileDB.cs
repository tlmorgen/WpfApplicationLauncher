using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.IO;
using WpfApplicationLauncher.StaticData;
using System.Collections.Concurrent;

namespace WpfApplicationLauncher.DataSourcing.FileSearchProvider
{
    /// <summary>
    /// Simple in-memory file database with real-time updates.
    /// </summary>
    public class FileDB : IDisposable, ISearchProvider
    {

        #region Static

        private enum FileDBDeltaResult
        {
            Success,
            FileAlreadyExists,
            FileDoesNotExist,
            Failure
        }

        private static FileDB lastDB = null;

        /// <summary>
        /// ISearchProvider helper.  Shared between FileDB and FileDBSearchQuery to search FileDB lists.
        /// </summary>
        /// <param name="provider">ISearchProvider providing the search.</param>
        /// <param name="sourceData">Data to search.</param>
        /// <param name="words">Search words.</param>
        /// <returns>FileDBSearchQuery representing relevent search results.</returns>
        internal static FileDBSearchQuery Search(FileDB provider, ICollection<StaticFileInfo> sourceData, params string[] words)
        {
            return new FileDBSearchQuery(
                provider,
                sourceData.Where(fi => words.All(w => fi.FullName.ToLower().IndexOf(w.ToLower()) > -1)).ToList());
        }

        /// <summary>
        /// Factory to ensure that FileDBs are not unnecessarily created.  
        /// Only one FileDB worth of history will be maintained as only one should be used at a time.
        /// </summary>
        /// <param name="cfg">Application configuration to base the DB on.</param>
        /// <returns>Either the existing or a new FileDB.</returns>
        public static FileDB CreateFileDB(Configuration cfg)
        {
            HashSet<string> fileExts = new HashSet<string>(cfg.SearchExtensions);
            HashSet<StaticDirectoryInfo> paths = new HashSet<StaticDirectoryInfo>(cfg.SearchPaths.Select(s => new StaticDirectoryInfo(s)));
            
            if (lastDB != null
                && !(paths.IsSubsetOf(lastDB.baseDirs)
                     && fileExts.IsSubsetOf(lastDB.extensions)))
            {
                lastDB.Dispose();
                lastDB = null;
            }
            
            if (lastDB == null)
            {
                lastDB = new FileDB(paths, fileExts);
            }

            return lastDB;
        }

        #endregion

        #region Variables

        protected List<FileSystemWatcher> watchers = new List<FileSystemWatcher>();
        protected ConcurrentDictionary<string, StaticFileInfo> database = new ConcurrentDictionary<string, StaticFileInfo>();
        protected HashSet<StaticDirectoryInfo> baseDirs = null;
        protected HashSet<string> extensions = null;
        protected bool initialScanDone = false;

        #endregion

        #region Properties

        public String Title
        {
            get
            {
                return "File System Database";
            }
        }

        #endregion

        #region Construction
        
        /// <summary>
        /// Single, private constructor.
        /// </summary>
        /// <param name="baseDirList">Collection of base directories to database.</param>
        /// <param name="extensionList">Collection of file extensions to match within the base directories.</param>
        private FileDB(IEnumerable<StaticDirectoryInfo> baseDirList, IEnumerable<string> extensionList = null)
        {
            if (baseDirList == null)
                throw new ArgumentException("baseDirList must be non-null");
            
            this.baseDirs = new HashSet<StaticDirectoryInfo>(baseDirList);

            if (extensionList != null)
            {
                this.extensions = new HashSet<string>(extensionList);
            }
            else
            {
                this.extensions = new HashSet<string>();
                this.extensions.Add("*");
            }

            ProcessAndWatchAll();
        }

        public void Dispose()
        {
            if (watchers != null)
            {
                foreach (FileSystemWatcher fsw in watchers)
                {
                    fsw.EnableRaisingEvents = false;
                    fsw.Dispose();
                }
                watchers.Clear();
                watchers = null;
            }
        }

        ~FileDB()
        {
            Dispose();
        }

        #endregion

        #region Event Handlers

        /// <summary>
        /// Handle a file system rename event.
        /// </summary>
        void FileRenamedHandler(object sender, RenamedEventArgs e)
        {
            ReplaceDatabaseEntry(e.OldFullPath, e.FullPath);
        }

        /// <summary>
        /// Handle a file system delete event.
        /// </summary>
        void FileDeletedHandler(object sender, FileSystemEventArgs e)
        {
            RemoveDatabaseEntry(e.FullPath);
        }

        /// <summary>
        /// Handle a file system create event.
        /// </summary>
        void FileCreatedHandler(object sender, FileSystemEventArgs e)
        {
            if (SatasfiesCriteria(e.FullPath))
            {
                StaticFileInfo fi = new StaticFileInfo(e.FullPath);
                database[fi.FullName] = fi;
            }
        }

        #endregion

        #region Methods

        /// <summary>
        /// Initialization is part of construction as this is not a Plugin.
        /// </summary>
        /// <returns>true</returns>
        bool ISearchProvider.Initialize()
        {
            return true;
        }

        /// <summary>
        /// Perform a query on the file database.
        /// </summary>
        /// <param name="words">Search words to use.</param>
        /// <returns>Search results.</returns>
        ISearchQuery ISearchProvider.Query(params string[] words)
        {
            return Search(this, this.database.Values, words);
        }

        /// <summary>
        /// Start scan of all files beneath the base dirs and watch for file system changes.
        /// </summary>
        private void ProcessAndWatchAll()
        {
            baseDirs.RemoveWhere(d => d == null);
            baseDirs.RemoveWhere(d => !d.Exists);

            if (baseDirs.Count < 1)
                return;

            foreach (StaticDirectoryInfo dir in baseDirs)
            {
                ProcessDir(dir);
            }

            foreach (FileSystemWatcher watcher in watchers)
            {
                watcher.EnableRaisingEvents = true;
            }

            this.initialScanDone = true;
        }

        /// <summary>
        /// Scan base directory based on configured file extensions.
        /// </summary>
        /// <param name="dir">Base directory info.</param>
        /// <returns>Update watcher for this base dir.</returns>
        private FileSystemWatcher ProcessDir(StaticDirectoryInfo dir)
        {
            extensions
                .SelectMany(ext => dir.GetFiles("*" + ext, SearchOption.AllDirectories))
                .ToList()
                .ForEach(fi => database[fi.FullName] = fi);

            FileSystemWatcher watcher = new FileSystemWatcher(dir.FullName);
            watcher.IncludeSubdirectories = true;
            watcher.Created += FileCreatedHandler;
            watcher.Deleted += FileDeletedHandler;
            watcher.Renamed += FileRenamedHandler;

            watchers.Add(watcher);

            return watcher;
        }

        /// <summary>
        /// Replace an existing file entry in the file db.
        /// </summary>
        /// <param name="oldEntry">Old path.</param>
        /// <param name="newEntry">New path.</param>
        /// <returns>True or false that both operations were successful.</returns>
        private bool ReplaceDatabaseEntry(string oldEntry, string newEntry)
        {
            StaticFileInfo fi;
            return database.TryRemove(oldEntry, out fi) && database.TryAdd(newEntry, new StaticFileInfo(newEntry));
        }

        /// <summary>
        /// Remove an entry from the file db.
        /// </summary>
        /// <param name="entry">File path.</param>
        /// <returns>True of false that it was removed.</returns>
        private bool RemoveDatabaseEntry(string entry)
        {
            StaticFileInfo fi;
            return database.TryRemove(entry, out fi);
        }

        /// <summary>
        /// Ensure that file updates satasfy the file extension criteria.
        /// </summary>
        /// <param name="path">File path.</param>
        /// <returns>Should path be in the database.</returns>
        private bool SatasfiesCriteria(string path)
        {
            if (Directory.Exists(path))
            {
                return false;
            }
            else
            {
                if (extensions != null)
                {
                    if (extensions.Any(ext => path.EndsWith(ext)))
                    {
                        return true;
                    }
                }
                else
                {
                    return true;
                }
            }
            return false;
        }

        #endregion
    }
}
