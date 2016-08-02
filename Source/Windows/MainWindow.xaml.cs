namespace SQLiteLogViewer
{
    using Toolkit;
    using System.Windows;
    using ViewModels;
    using System;
    using System.Diagnostics.CodeAnalysis;
    using SQLiteDebugger;
    using System.Windows.Controls.Primitives;
    using System.Windows.Controls;
    using System.ComponentModel;
    using System.Linq;

    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        private readonly App app = Application.Current as App;
        private EventAggregator events = new EventAggregator(new WpfDispatcher());
        private IConductor conductor = new Conductor();
        private DebugClient client;

        [SuppressMessage("Microsoft.Reliability", "CA2000:Dispose objects before losing scope", Justification = "Owernship passed to MainViewModel")]
        public MainWindow()
        {
            this.InitializeComponent();

            this.client = new DebugClient(this.events);

            var logViewModel = new LogViewModel(this.events, this.client) { Conductor = this.conductor };
            var mainViewModel = new MainViewModel(this.events, this.client) { Conductor = this.conductor, LogViewModel = logViewModel };
            this.Log.Sorting += this.Log_Sorting;

            this.DataContext = mainViewModel;
            this.client.Connect("localhost", this.app.Port);

            this.Closing += (sender, e) => e.Cancel = !mainViewModel.Cleanup();
        }

        private void Log_Sorting(object sender, DataGridSortingEventArgs e)
        {
            if (e.Column.SortDirection != ListSortDirection.Descending)
            {
                return;
            }

            var oldSort = this.Log.Items.SortDescriptions
                .FirstOrDefault((sort) => sort.PropertyName == e.Column.SortMemberPath);
            if (oldSort == null)
            {
                return;
            }

            e.Column.SortDirection = null;

            this.Log.Items.SortDescriptions.Remove(oldSort);
            this.Log.Items.Refresh();

            e.Handled = true;
        }

        private void Log_ScrollChanged(object sender, ScrollChangedEventArgs e)
        {
            if (this.Log.Items.Count == 0)
            {
                return;
            }

            if (e.ExtentHeightChange == 0 && e.ViewportHeightChange == 0)
            {
                return;
            }

            var oldExtentHeight = e.ExtentHeight - e.ExtentHeightChange;
            var oldVerticalOffset = e.VerticalOffset - e.VerticalChange;
            var oldViewportHeight = e.ViewportHeight - e.ViewportHeightChange;
            if (oldVerticalOffset + oldViewportHeight + 5 >= oldExtentHeight)
            {
                var info = this.Log.Items[this.Log.Items.Count - 1];
                this.Log.ScrollIntoView(info);
            }
        }

        private class WpfDispatcher : IDispatcher
        {
            public void Invoke(Action action)
            {
                Application.Current.Dispatcher.Invoke(action);
            }
        }
    }
}
