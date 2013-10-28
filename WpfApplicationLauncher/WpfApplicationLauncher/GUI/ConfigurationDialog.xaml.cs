using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Interop;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;
using System.Data;
using System.IO;
using System.Threading;
using System.ComponentModel;
using Microsoft.Win32;

using WpfApplicationLauncher.DataSourcing;
using WpfApplicationLauncher.DataSourcing.FileSearchProvider;
using WpfApplicationLauncher.StaticData;

namespace WpfApplicationLauncher.GUI
{
    /// <summary>
    /// Interaction logic for ConfigurationDialog.xaml
    /// </summary>
    public partial class ConfigurationDialog : Window
    {
        #region Variables

        private object columnAnimationLock = new object();
        private int showColumn = -1;
        private int hideColumn = -1;
        private FileDB fileDB;
        private Configuration newSettings = Configuration.Instance;

        #endregion

        #region Init

        public ConfigurationDialog()
        {
            fileDB = SearchDataHub.GetProvider<FileDB>();

            InitializeComponent();
            BindGuiData();            
            WireEvents();            
        }

        private void BindGuiData()
        {
            this.lstExtensions.ItemsSource = newSettings.SearchExtensions;
            this.lstFileActions.ItemsSource = newSettings.FileActions;
            this.lstPaths.ItemsSource = newSettings.SearchPaths;
            this.txtNewSearchHK.Tag = newSettings.NewSearchHotKey;
            this.txtNewSearchHK.Text = TypeDescriptor.GetConverter(newSettings.NewSearchHotKey).ConvertToString(newSettings.NewSearchHotKey);
            this.txtLastSearchHK.Tag = newSettings.LastSearchHotKey;
            this.txtLastSearchHK.Text = TypeDescriptor.GetConverter(newSettings.LastSearchHotKey).ConvertToString(newSettings.LastSearchHotKey);
        }

        private void WireEvents()
        {
            this.Loaded += ConfigurationDialog_Loaded;

            this.PreviewKeyDown += ConfigurationDialog_PreviewKeyDown;

            this.lstSettings.SelectionChanged += lstSettings_SelectionChanged;
            this.lstFileActions.SelectionChanged += lstFileActions_SelectionChanged;

            this.btnAddExt.Click += btnAddExt_Click;
            this.btnDelExt.Click += btnDelExt_Click;

            this.btnAddPath.Click += btnAddPath_Click;
            this.btnDelPath.Click += btnDelPath_Click;

            this.lblFilePath.MouseDown += lblFilePath_MouseDown;
            this.lblFileDirPath.MouseDown += lblFileDirPath_MouseDown;

            this.btnBrowseExe.Click += btnBrowseExe_Click;
            this.btnNewFileAction.Click += btnNewFileAction_Click;
            this.btnSaveFileAction.Click += btnSaveFileAction_Click;
            this.btnDeleteFileAction.Click += btnDeleteFileAction_Click;

            this.txtNewSearchHK.KeyDown += HotKeyTextBox_KeyDown;
            this.txtLastSearchHK.KeyDown += HotKeyTextBox_KeyDown;
            this.txtNewSearchHK.LostKeyboardFocus += txtNewSearchHK_LostKeyboardFocus;
            this.txtLastSearchHK.LostKeyboardFocus += txtLastSearchHK_LostKeyboardFocus;

            this.btnSave.Click += btnSave_Click;
            this.btnCancel.Click += btnCancel_Click;
        }

        #endregion

        #region Events

        void ConfigurationDialog_Loaded(object sender, RoutedEventArgs e)
        {
            Win32.HideFromAltTab(this);
            this.lstSettings.SelectedIndex = this.lstSettings.Items.Count > 0 ? 0 : -1;
        }

        void lstSettings_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (e.AddedItems.Count != 1)
                return;

            ListBoxItem item = (ListBoxItem)e.AddedItems[0];

            ShowMainGridColumn(int.Parse((string)item.Tag));
        }

        void btnBrowseExe_Click(object sender, RoutedEventArgs e)
        {
            OpenFileDialog dlg = new OpenFileDialog();
            dlg.Multiselect = false;

            dlg.ShowDialog();

            if (!File.Exists(dlg.FileName))
                return;

            this.txtFileExeFormat.Text = "\"" + dlg.FileName + "\"";
        }

        void btnDeleteFileAction_Click(object sender, RoutedEventArgs e)
        {
            if (this.lstFileActions.SelectedItem == null)
                return;

            ExecutableContext fa = this.lstFileActions.SelectedItem as ExecutableContext;
            if (fa == null)
                return;

            RemoveFileAction(fa);
        }

        void ConfigurationDialog_PreviewKeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Escape)
            {
                this.Close();
            }
        }

        void lstFileActions_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (this.lstFileActions.SelectedItem == null)
                return;

            ExecutableContext fa = this.lstFileActions.SelectedItem as ExecutableContext;
            if (fa == null)
                return;

            this.txtFileActionName.Text = fa.Name;
            this.txtFileExeFormat.Text = fa.Executable;
            this.txtFileArgFormat.Text = fa.Arguments;
        }

        void btnSaveFileAction_Click(object sender, RoutedEventArgs e)
        {
            if (String.IsNullOrEmpty(this.txtFileActionName.Text) ||
                String.IsNullOrEmpty(this.txtFileExeFormat.Text))
            {
                return;
            }

            ExecutableContext nfa = new ExecutableContext();
            nfa.Name = this.txtFileActionName.Text;
            nfa.Executable = this.txtFileExeFormat.Text;
            nfa.Arguments = this.txtFileArgFormat.Text;
            newSettings.FileActions.Add(nfa);
        }

        void btnNewFileAction_Click(object sender, RoutedEventArgs e)
        {
            ClearForm();
        }

        private void lblFilePath_MouseDown(object sender, MouseButtonEventArgs e)
        {
            Label lbl = (Label)sender;
            DragDropEffects result = DragDrop.DoDragDrop(lbl, "{0}", DragDropEffects.Copy);
        }

        private void lblFileDirPath_MouseDown(object sender, MouseButtonEventArgs e)
        {
            Label lbl = (Label)sender;
            DragDropEffects result = DragDrop.DoDragDrop(lbl, "{1}", DragDropEffects.Copy);
        }

        void btnDelPath_Click(object sender, RoutedEventArgs e)
        {
            if (this.lstPaths.SelectedItem == null)
                return;

            DeletePath(this.lstPaths.SelectedItem as string);
        }

        void btnAddPath_Click(object sender, RoutedEventArgs e)
        {
            IntPtr mainWindowPtr = new WindowInteropHelper(this).Handle;
            System.Windows.Forms.FolderBrowserDialog folderDlg = new System.Windows.Forms.FolderBrowserDialog();
            folderDlg.ShowNewFolderButton = true;
            if (folderDlg.ShowDialog(new Win32.Win32Window(mainWindowPtr)) != System.Windows.Forms.DialogResult.OK)
                return;

            AddPath(folderDlg.SelectedPath);
        }

        void btnDelExt_Click(object sender, RoutedEventArgs e)
        {
            if (this.lstExtensions.SelectedItem == null)
                return;

            DeleteExt(this.lstExtensions.SelectedItem as string);
        }

        void btnAddExt_Click(object sender, RoutedEventArgs e)
        {
            if (this.txtNewExt.Text == string.Empty)
                return;

            AddExt(this.txtNewExt.Text);
        }

        void HotKeyTextBox_KeyDown(object sender, KeyEventArgs e)
        {
            TextBox txt = sender as TextBox;
            if (txt == null)
                return;

            e.Handled = true;
            System.Windows.Forms.Keys keys = (System.Windows.Forms.Keys)KeyInterop.VirtualKeyFromKey(e.Key);
            keys = keys | System.Windows.Forms.Control.ModifierKeys;
            txt.Tag = keys;
            txt.Text = TypeDescriptor.GetConverter(keys).ConvertToString(keys);
        }

        void txtLastSearchHK_LostKeyboardFocus(object sender, KeyboardFocusChangedEventArgs e)
        {
            newSettings.LastSearchHotKey = ((System.Windows.Forms.Keys)txtLastSearchHK.Tag) | System.Windows.Forms.Keys.None;
        }

        void txtNewSearchHK_LostKeyboardFocus(object sender, KeyboardFocusChangedEventArgs e)
        {
            newSettings.NewSearchHotKey = ((System.Windows.Forms.Keys)txtNewSearchHK.Tag) | System.Windows.Forms.Keys.None;
        }

        void btnSave_Click(object sender, RoutedEventArgs e)
        {
            this.Close();
            Configuration.Instance = newSettings;
        }

        void btnCancel_Click(object sender, RoutedEventArgs e)
        {
            this.newSettings = Configuration.Instance;
            this.Close();
        }

        #endregion

        #region Helpers

        private void ClearForm()
        {
            this.txtFileActionName.Text = string.Empty;
            this.txtFileExeFormat.Text = string.Empty;
            this.txtFileArgFormat.Text = string.Empty;
            this.lstFileActions.SelectedIndex = -1;
        }

        private void RemoveFileAction(ExecutableContext fa)
        {
            newSettings.FileActions.Remove(fa);
        }

        private void ShowMainGridColumn(int col)
        {
            this.showColumn = col;
            for (int i = 1; i < this.MainGrid.ColumnDefinitions.Count; i++)
            {
                if (this.MainGrid.ColumnDefinitions[i].Width.Value != 0)
                {
                    this.hideColumn = i;
                    break;
                }
            }

            if (this.hideColumn >= 0)
            {
                this.MainGrid.ColumnDefinitions[this.hideColumn].Width = new GridLength(0, GridUnitType.Star);
            }
            if (this.showColumn >= 0)
            {
                this.MainGrid.ColumnDefinitions[this.showColumn].Width = new GridLength(1, GridUnitType.Star);
            }

            this.hideColumn = -1;
            this.showColumn = -1;
        }

        private void AddPath(string path)
        {
            newSettings.SearchPaths.Add(path);
        }

        private void DeletePath(string path)
        {
            newSettings.SearchPaths.Remove(path);
        }

        private void AddExt(string ext)
        {
            Match m = Regex.Match(ext, @"[a-zA-Z0-9]+$");
            if (m.Success)
            {
                newSettings.SearchExtensions.Add("." + m.Groups[0].Value.ToLower());
            }
        }

        private void DeleteExt(string ext)
        {
            newSettings.SearchExtensions.Remove(ext);
        }

        #endregion
    }
}
