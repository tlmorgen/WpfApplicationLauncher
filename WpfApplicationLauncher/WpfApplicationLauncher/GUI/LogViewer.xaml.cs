using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;

using WpfApplicationLauncher.Logging;

namespace WpfApplicationLauncher.GUI
{
    /// <summary>
    /// Interaction logic for LogViewer.xaml
    /// </summary>
    public partial class LogViewer : Window
    {
        public LogViewer()
        {
            InitializeComponent();
            WireEvents();
        }

        private void WireEvents()
        {
            Loaded += LogViewer_Loaded;
            PreviewKeyUp += LogViewer_PreviewKeyUp;
        }

        void LogViewer_PreviewKeyUp(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Escape)
            {
                this.Close();
            }
        }

        void LogViewer_Loaded(object sender, RoutedEventArgs e)
        {
            this.gridMain.ItemsSource = Log.LogRoll;
        }
    }
}
