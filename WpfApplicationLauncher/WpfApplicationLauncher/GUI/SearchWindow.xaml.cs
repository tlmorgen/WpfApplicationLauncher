using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Interop;
using System.Windows.Threading;
using WpfApplicationLauncher.StaticData;
using WpfApplicationLauncher.DataSourcing;
using WpfApplicationLauncher.DataSourcing.FileSearchProvider;

namespace WpfApplicationLauncher.GUI
{
    /// <summary>
    /// Main Window - Search Window
    /// </summary>
    public partial class SearchWindow : Window
    {
        #region Static

        /// <summary>
        /// Lightweight storage of the state of a SearchWindow for recreation.
        /// </summary>
        public class SearchWindowAshes
        {
            public string SearchText { get; private set; }
            public IEnumerable ItemSource { get; private set; }
            public int SelectedIndex { get; private set; }

            public SearchWindowAshes(SearchWindow copyFrom)
            {
                SearchText = copyFrom.SearchTextBox.Text;
                ItemSource = copyFrom.ResultsListBox.ItemsSource;
                SelectedIndex = copyFrom.ResultsListBox.SelectedIndex;
            }
        }

        #endregion

        #region Variables

        private WindowInteropHelper myIntOp = null;
        private bool persistWindow = false;
        private string lastSuccessfulSearchString = "";

        #endregion

        #region Construction

        public SearchWindow()
        {
            InitializeComponent();

            WireEvents();
        }

        /// <summary>
        /// Copy constructor from previous SearchWindow
        /// </summary>
        /// <param name="CopyFrom"></param>
        public SearchWindow(SearchWindowAshes CopyFrom) : this()
        {
            if (CopyFrom != null)
            {
                this.ResultsListBox.Dispatcher.BeginInvoke(new Action(delegate()
                {
                    this.SearchTextBox.Text = CopyFrom.SearchText;
                    this.SearchTextBox.Select(this.SearchTextBox.Text.Length, 0);
                    this.ResultsListBox.ItemsSource = CopyFrom.ItemSource;
                    this.ResultsListBox.SelectedIndex = CopyFrom.SelectedIndex;
                }), DispatcherPriority.Loaded);
            }
        }

        private void WireEvents()
        {
            this.Loaded += SearchWindow_Loaded;
            this.KeyDown += SearchWindow_KeyDown;
            this.Activated += SearchWindow_Activated;
            this.Deactivated += SearchWindow_Deactivated;            

            this.SearchTextBox.LostKeyboardFocus += SearchTextBox_LostKeyboardFocus;
            this.SearchTextBox.KeyDown += SearchTextBox_KeyDown;
            this.SearchTextBox.PreviewKeyDown += SearchTextBox_KeyDown;
            this.SearchTextBox.TextChanged += SearchTextBox_TextChanged;

            this.ResultsListBox.SelectionChanged += ResultsListBox_SelectionChanged;

            Configuration.Changed += Configuration_Changed;
        }

        #endregion

        #region Event Handlers

        void SearchWindow_Loaded(object sender, RoutedEventArgs e)
        {
            myIntOp = new WindowInteropHelper(this);
            Win32.HideFromAltTab(myIntOp.Handle);
            LoadFileActionsList();
            ResultsListBox_SourceUpdated();
            this.Activate();
            this.SearchTextBox.Focus();
            this.SearchTextBox.SelectionStart = this.SearchTextBox.Text.Length;
        }

        void SearchWindow_Activated(object sender, EventArgs e)
        {
            FadeIn(null);
        }

        void SearchWindow_Deactivated(object sender, EventArgs e)
        {
            if (!this.persistWindow)
            {
                this.Hide();
                this.Close();
            }
        }

        void SearchWindow_KeyDown(object sender, KeyEventArgs e)
        {
            e.Handled = false;

            switch (e.Key)
            {
                case Key.Escape:
                    SearchWindow_Deactivated(this, new EventArgs());
                    e.Handled = true;
                    break;
            }
        }

        void SearchTextBox_LostKeyboardFocus(object sender, KeyboardFocusChangedEventArgs e)
        {
            this.SearchTextBox.Dispatcher.BeginInvoke(new Action(() => this.SearchTextBox.Focus()));
        }

        void SearchTextBox_KeyDown(object sender, KeyEventArgs e)
        {
            e.Handled = true;
            
            switch (e.Key)
            {
                case Key.Enter:
                    if (e.KeyboardDevice.IsKeyDown(Key.RightShift) ||
                        e.KeyboardDevice.IsKeyDown(Key.LeftShift))
                    {
                        LaunchSelectedItem(true);
                    }
                    else
                    {
                        LaunchSelectedItem(false);
                    }
                    break;
                case Key.Down:
                    DoKeyDown();
                    break;
                case Key.Up:
                    DoKeyUp();
                    break;
                case Key.Tab:
                    if (e.KeyboardDevice.IsKeyDown(Key.LeftShift) ||
                        e.KeyboardDevice.IsKeyDown(Key.RightShift))
                    {
                        HideFileAdvanced(FileAdvancedChangeSpeed.Slow);
                    }
                    else
                    {
                        ShowFileAdvanced(FileAdvancedChangeSpeed.Slow);
                    }
                    break;
                case Key.A:
                    if (e.KeyboardDevice.Modifiers == ModifierKeys.Control)
                        SelectAllItems();
                    else
                        e.Handled = false;
                    break;
                default:
                    e.Handled = false;
                    break;
            }
        }

        void SearchTextBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            DoSearch();
        }

        private void imConfig_MouseDown(object sender, MouseButtonEventArgs e)
        {
            ShowConfig();
        }

        void ResultsListBox_SourceUpdated()
        {
            if (this.ResultsListBox.HasItems)
            {
                this.ResultsListBox.SelectedIndex = 0;

                this.ResultsListBox.Height = double.NaN;
                this.ResultsListBox.Margin = new Thickness(10);
            }
            else
            {
                this.ResultsListBox.Height = 0;
                this.ResultsListBox.Margin = new Thickness(0);
                HideFileAdvanced(FileAdvancedChangeSpeed.Fast);
            }
        }

        void ResultsListBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            this.IconImage.Source = null;
            HideFileAdvanced(FileAdvancedChangeSpeed.Slow);

            if (this.ResultsListBox.SelectedItem == null)
                return;

            StaticFileInfo fi = this.ResultsListBox.SelectedItem as StaticFileInfo;

            if (fi == null)
                return;

            this.IconImage.Source = fi.ImageSource;
        }

        void Configuration_Changed(Configuration oldConfig, Configuration newConfig)
        {
            LoadFileActionsList();
        }

        #endregion

        #region Methods

        private void DoSearch()
        {
            string searchText = this.SearchTextBox.Text;

            if (String.IsNullOrWhiteSpace(searchText))
            {
                ClearSearchResults();
            }
            else
            {
                IEnumerable<ISearchResult> searchResults = SearchDataHub.Query(searchText);

                if (searchResults.Count() > 0)
                {
                    this.lastSuccessfulSearchString = searchText;
                }

                PopulateSearchResults(searchResults);
            }

            DrawSearchStringValidity();
        }

        private void ShowConfig()
        {
            this.FadeOut(new EventHandler((s,e) => {
                this.persistWindow = true;
                this.Hide();
                Configuration.ShowConfigWindow();
                this.Show();
                this.FadeIn(new EventHandler((s1,e1) => {                    
                    this.persistWindow = false;
                }));
            }));
        }

        public void ClearSearch()
        {
            this.SearchTextBox.Text = string.Empty;
        }

        #endregion

        #region Helpers

        private void SelectAllItems()
        {
            this.ResultsListBox.SelectAll();
        }

        private void LoadFileActionsList()
        {
            this.FileActionList.ItemsSource = null;
            this.FileActionList.DisplayMemberPath = "Name";

            ObservableCollection<ExecutableContext> ocActionList = new ObservableCollection<ExecutableContext>(Configuration.Instance.FileActions);

            this.FileActionList.ItemsSource = ocActionList;
        }

        private void PopulateSearchResults(IEnumerable<ISearchResult> results)
        {
            this.ResultsListBox.ItemsSource = results;
            ResultsListBox_SourceUpdated();
        }

        public void ClearSearchResults()
        {
            if (!String.IsNullOrWhiteSpace(this.SearchTextBox.Text))
            {
                this.SearchTextBox.Text = string.Empty;
            }

            if (this.ResultsListBox.ItemsSource == null)
            {
                this.ResultsListBox.Items.Clear();
            }
            else
            {
                this.ResultsListBox.ItemsSource = null;
            }

            ResultsListBox_SourceUpdated();
        }

        private void DoKeyUp()
        {
            if (this.FileActionList.Width == 0)
            {
                DoKeyUp(this.ResultsListBox);
            }
            else
            {
                DoKeyUp(this.FileActionList);
            }
        }

        private void DoKeyUp(ListBox listBox)
        {
            if (listBox.Items.Count == 0)
                return;

            if (listBox.SelectedIndex - 1 < 0)
                return;

            listBox.SelectedIndex--;
            listBox.ScrollIntoView(listBox.SelectedItem);
        }

        private void DoKeyDown()
        {
            if (this.FileActionList.Width == 0)
            {
                DoKeyDown(this.ResultsListBox);
            }
            else
            {
                DoKeyDown(this.FileActionList);
            }
        }

        private void DoKeyDown(ListBox listBox)
        {
            if (listBox.Items.Count == 0)
                return;

            if (listBox.SelectedIndex + 1 >= listBox.Items.Count)
                return;

            listBox.SelectedIndex++;
            listBox.ScrollIntoView(listBox.SelectedItem);
        }

        private void LaunchSelectedItem(bool persistwindow = false)
        {
            this.persistWindow = persistwindow;

            if (!persistwindow)
            {
                SearchWindow_Deactivated(this, new EventArgs());
            }
                    
            ExecutableContext fa = this.FileActionList.SelectedItem as ExecutableContext;
            Helpers.Launch(fa, this.ResultsListBox.SelectedItems.Cast<ISearchResult>().Select(s => s.URI), persistwindow);

            this.persistWindow = false;

            if (persistwindow)
            {
                this.Activate();
            }
            else
            {
                SearchWindow_Deactivated(this, new EventArgs());
            }
        }

        private void DrawSearchStringValidity()
        {
            if (this.SearchTextBox.Text.Length > 0)
            {
                if (this.SearchTextBox.Text == this.lastSuccessfulSearchString)
                {
                    Rect r = this.SearchTextBox.GetRectFromCharacterIndex(this.SearchTextBox.Text.Length - 1);
                    this.SearchTextProgressBar.Background = new SolidColorBrush(System.Windows.Media.Colors.White);
                    this.SearchTextProgressBar.Value = 100 * (r.BottomRight.X / (this.SearchTextBox.ActualWidth - this.SearchTextBox.Padding.Right - this.SearchTextBox.Padding.Left - 1));
                }
                else
                {
                    this.SearchTextProgressBar.Background = new SolidColorBrush(System.Windows.Media.Colors.Red);
                }
            }
            else
            {
                this.SearchTextProgressBar.Background = new SolidColorBrush(System.Windows.Media.Colors.White);
                this.SearchTextProgressBar.Value = 0;
            }
        }

        private void FadeIn(EventHandler OnComplete)
        {
            DoubleAnimation daFadeIn = new DoubleAnimation(0, 1, new Duration(new TimeSpan(0, 0, 0, 0, 150)));

            if (OnComplete != null)
            {
                daFadeIn.Completed += OnComplete;
            }

            this.BeginAnimation(Window.OpacityProperty, daFadeIn, HandoffBehavior.Compose);
        }

        private void FadeOut(EventHandler OnComplete)
        {
            DoubleAnimation daFadeOut = new DoubleAnimation(1, 0, new Duration(new TimeSpan(0, 0, 0, 0, 150)));

            if (OnComplete != null)
            {
                daFadeOut.Completed += OnComplete;
            }

            this.BeginAnimation(SearchWindow.OpacityProperty, daFadeOut, HandoffBehavior.Compose);
        }

        #endregion

        #region FileAdvanced

        private enum FileAdvancedChangeSpeed { Fast, Slow };

        private void ShowFileAdvanced(FileAdvancedChangeSpeed speed)
        {
            if (this.FileActionList.Width != 0)
                return;

            if (this.ResultsListBox.SelectedItem == null)
                return;

            if (speed == FileAdvancedChangeSpeed.Slow)
            {
                this.FileActionList.Margin = new Thickness(0, 10, 10, 10);
                this.FileActionList.Width = double.NaN;
                this.FileActionList.Height = double.NaN;

                GridLengthAnimation animShowAdvCol = new GridLengthAnimation();
                animShowAdvCol.From = new GridLength(0);
                animShowAdvCol.To = new GridLength(6, GridUnitType.Star);
                animShowAdvCol.Duration = new Duration(new TimeSpan(0, 0, 0, 0, 300));
                animShowAdvCol.Completed += new EventHandler(animShowFileAdvanced_Completed);

                this.ResultsGrid.ColumnDefinitions[1].BeginAnimation(ColumnDefinition.WidthProperty, animShowAdvCol);
            }
            else if (speed == FileAdvancedChangeSpeed.Fast)
            {
                this.FileActionList.Margin = new Thickness(0, 10, 10, 10);
                this.FileActionList.Width = double.NaN;
                this.FileActionList.Height = double.NaN;
                this.ResultsGrid.ColumnDefinitions[1].Width = new GridLength(0.5, GridUnitType.Star);
            }
        }

        private void HideFileAdvanced(FileAdvancedChangeSpeed speed)
        {
            if (this.FileActionList.Width == 0)
                return;

            if (speed == FileAdvancedChangeSpeed.Slow)
            {
                GridLengthAnimation animShowAdvCol = new GridLengthAnimation();
                animShowAdvCol.From = new GridLength(6, GridUnitType.Star);
                animShowAdvCol.To = new GridLength(0);
                animShowAdvCol.Duration = new Duration(new TimeSpan(0, 0, 0, 0, 300));
                animShowAdvCol.Completed += new EventHandler(animHideFileAdvanced_Completed);

                this.ResultsGrid.ColumnDefinitions[1].BeginAnimation(ColumnDefinition.WidthProperty, animShowAdvCol);
            }
            else if (speed == FileAdvancedChangeSpeed.Fast)
            {
                this.FileActionList.Margin = new Thickness(0);
                this.FileActionList.Width = 0;
                this.FileActionList.Height = 0;
                this.ResultsGrid.ColumnDefinitions[1].Width = new GridLength(0);
            }
        }

        void animHideFileAdvanced_Completed(object sender, EventArgs e)
        {
            this.FileActionList.Margin = new Thickness(0);
            this.FileActionList.Width = 0;
            this.FileActionList.Height = 0;
        }

        void animShowFileAdvanced_Completed(object sender, EventArgs e)
        {
            this.FileActionList.SelectedIndex = 0;
            this.FileActionList.Focus();
        }

        #endregion

    }
}
