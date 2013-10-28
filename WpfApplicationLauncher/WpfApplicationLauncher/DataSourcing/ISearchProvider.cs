using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace WpfApplicationLauncher.DataSourcing
{
    /// <summary>
    /// API entry point to provide search results to the application.
    /// </summary>
    public interface ISearchProvider
    {
        /// <summary>
        /// Used to display search provider in UI.
        /// </summary>
        String Title { get; }

        /// <summary>
        /// Ran after discovery by API.
        /// </summary>
        bool Initialize();

        /// <summary>
        /// Perform a search.
        /// </summary>
        /// <param name="words">Search words to use.</param>
        /// <returns><see cref="ISearchQuery"/> containing results.</returns>
        ISearchQuery Query(params String[] words);
    }
}
