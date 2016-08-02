namespace SQLiteLogViewer
{
    using Toolkit;
    using System.Windows;
    using ViewModels;
    using System;
    using System.Diagnostics.CodeAnalysis;
    using SQLiteDebugger;
    using System.Windows.Controls.Primitives;

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
            this.Log.ItemContainerGenerator.StatusChanged += ItemContainerGenerator_StatusChanged;

            this.DataContext = mainViewModel;
            this.client.Connect("localhost", this.app.Port);

            this.Closing += (sender, e) => e.Cancel = !mainViewModel.Cleanup();
        }

        private void ItemContainerGenerator_StatusChanged(object sender, EventArgs e)
        {
            if (this.Log.ItemContainerGenerator.Status == GeneratorStatus.ContainersGenerated)
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
