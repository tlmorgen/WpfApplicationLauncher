using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;

namespace WpfApplicationLauncher.DataSourcing.FileSearchProvider
{

    /// <summary>
    /// Thin interface to <see cref="System.IO.DirectoryInfo"/>.  
    /// The former class was over-weight for the need.
    /// </summary>
    public class StaticDirectoryInfo : IEquatable<StaticDirectoryInfo>
    {
        #region Variables

        private string fullName = string.Empty;
        private string name = string.Empty;

        #endregion

        #region Ctor

        public StaticDirectoryInfo(string filePath)
        {
            fullName = Path.GetFullPath(filePath);
            name = Path.GetFileName(fullName);
        }

        #endregion

        #region Methods

        public string FullName
        {
            get
            {
                return this.fullName;
            }
        }

        public bool Exists
        {
            get
            {
                return Directory.Exists(this.fullName);
            }
        }

        public override string ToString()
        {
            return this.name;
        }

        /// <summary>
        /// <code>System.IO.Directory.GetFiles</code> with results as <code>StaticFileInfo</code>.
        /// </summary>
        /// <param name="searchPattern">The search string to match against the names of files in path. The parameter cannot end in two periods ("..") or contain two periods ("..") followed by System.IO.Path.DirectorySeparatorChar or System.IO.Path.AltDirectorySeparatorChar, nor can it contain any of the characters in System.IO.Path.InvalidPathChars.</param>
        /// <param name="searchOptions">One of the System.IO.SearchOption values that specifies whether the search operation should include all subdirectories or only the current directory.</param>
        /// <returns>Enumerable of matching file names.</returns>
        public IEnumerable<StaticFileInfo> GetFiles(string searchPattern, SearchOption searchOptions)
        {
            return Directory.GetFiles(this.fullName, searchPattern, searchOptions).Select(s => new StaticFileInfo(s));
        }

        public override bool Equals(object obj)
        {
            if (obj == null) return false;
            StaticDirectoryInfo other = obj as StaticDirectoryInfo;
            if (other == null) return false;
            return this.fullName.Equals(other.fullName);
        }

        public override int GetHashCode()
        {
            return fullName.GetHashCode();
        }

        public bool Equals(StaticDirectoryInfo other)
        {
            if (other == null) return false;
            return this.fullName.Equals(other.fullName);
        }

        #endregion
    }
}
