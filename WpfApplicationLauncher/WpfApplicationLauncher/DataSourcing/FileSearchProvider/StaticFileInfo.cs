using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;
using System.Windows;
using System.Windows.Media;
using System.Runtime.InteropServices;
using System.Drawing;
using System.ComponentModel;
using WpfApplicationLauncher.GUI;

namespace WpfApplicationLauncher.DataSourcing.FileSearchProvider
{
    /// <summary>
    /// Thin interface to <see cref="System.IO.FileInfo"/>.  
    /// The former class was over-weight for the need.
    /// </summary>
    public class StaticFileInfo : ISearchResult
    {
        #region Variables

        private string fullName = string.Empty;
        private string name = string.Empty;
        private Uri url = null;
        private Icon icon = null;

        #endregion

        #region Ctor

        public StaticFileInfo(string filePath)
        {
            fullName = Path.GetFullPath(filePath);
            name = Path.GetFileName(fullName);
            url = new Uri(fullName);
        }


        #endregion

        #region Properties

        public string Title
        {
            get { return Name; }
        }

        public Uri URI
        {
            get { return url; }
        }

        public string FullName
        {
            get { return this.fullName; }
        }

        public string Name
        {
            get { return this.name; }
        }

        public string DirectoryName
        {
            get
            {
                return Path.GetDirectoryName(this.fullName);
            }
        }

        public StaticDirectoryInfo Directory
        {
            get
            {
                return new StaticDirectoryInfo(Path.GetDirectoryName(this.fullName));
            }
        }

        public bool Exists
        {
            get
            {
                return File.Exists(this.fullName);
            }
        }

        public ImageSource ImageSource
        {
            get
            {
                if (this.icon == null)
                {
                    this.icon = Win32.GetIcon(this.fullName);
                }
                return this.icon.AsImageSource();
            }
        }

        #endregion

        #region Overrides 

        public override string ToString()
        {
            return this.name;
        }

        #endregion
    }
}
