namespace SQLiteLogViewer
{
    using Toolkit;
    using System.Windows;
    using ViewModels;
    using System;
    using System.Diagnostics.CodeAnalysis;
    using SQLiteDebugger;

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
            this.events.Subscribe<EntryViewModel>(this.EntryAdded, ThreadAffinity.UIThread);

            this.DataContext = mainViewModel;
            this.client.Connect("localhost", this.app.Port);

            this.Closing += (sender, e) => e.Cancel = !mainViewModel.Cleanup();
        }

        private void EntryAdded(EntryViewModel entry)
        {
            this.Log.ScrollIntoView(entry);
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
