using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace WpfApplicationLauncher.DataSourcing.FileSearchProvider
{
    /// <summary>
    /// Container for FileDB search results.
    /// </summary>
    public class FileDBSearchQuery : ISearchQuery
    {
        private FileDB provider;
        private readonly List<StaticFileInfo> results = new List<StaticFileInfo>();

        public FileDBSearchQuery(FileDB provider, List<StaticFileInfo> results)
        {
            this.provider = provider;
            if (results != null)
            {
                this.results.AddRange(results);
            }
        }

        public ISearchProvider Provider
        {
            get { return provider; }
        }

        public IEnumerable<ISearchResult> Results
        {
            get { return new List<StaticFileInfo>(results); }
        }

        public ISearchQuery Filter(params string[] words)
        {
            return FileDB.Search(provider, results, words);
        }
    }
}
