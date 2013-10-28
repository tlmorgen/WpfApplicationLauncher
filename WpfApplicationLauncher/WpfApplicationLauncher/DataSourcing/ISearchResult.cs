using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace WpfApplicationLauncher.DataSourcing
{
    /// <summary>
    /// Individual search result from <see cref="ISearchProvider"/>.
    /// </summary>
    public interface ISearchResult
    {
        /// <summary>
        /// Display in UI.
        /// </summary>
        String Title { get; }

        /// <summary>
        /// Used to launch result.
        /// </summary>
        Uri URI { get; }
    }
}
