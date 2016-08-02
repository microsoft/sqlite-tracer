namespace SQLiteLogViewer
{
    using Toolkit;
    using System.Windows;
    using ViewModels;
    using System;

    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        private EventAggregator events = new EventAggregator(new WpfDispatcher());

        public MainWindow()
        {
            this.InitializeComponent();

            var app = Application.Current as App;
            var viewModel = new LogViewModel(this.events, app.Port) { Conductor = new Conductor() };
            this.events.Subscribe<EntryViewModel>(this.EntryAdded, ThreadAffinity.UIThread);
            this.DataContext = viewModel;
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
