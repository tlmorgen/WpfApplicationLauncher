using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace WpfApplicationLauncher.DataSourcing
{
    /// <summary>
    /// Returned by <see cref="ISearchProvider"/> with search results.
    /// </summary>
    public interface ISearchQuery
    {
        /// <summary>
        /// <see cref="ISearchProvider"/> used for search results catagorization.
        /// </summary>
        ISearchProvider Provider { get; }

        /// <summary>
        /// Search results.
        /// </summary>
        IEnumerable<ISearchResult> Results { get; }

        /// <summary>
        /// Search within the existing results.
        /// </summary>
        /// <param name="words">Search words.</param>
        /// <returns>Search results.</returns>
        ISearchQuery Filter(params String[] words);
    }
}
