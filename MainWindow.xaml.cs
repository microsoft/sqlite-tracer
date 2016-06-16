namespace SQLiteLogViewer
{
    using System.Windows;
    using ViewModels;

    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            this.InitializeComponent();

            var app = Application.Current as App;
            var viewModel = new LogViewModel(app.Port);
            viewModel.EntryAdded += this.Log_EntryAdded;
            this.DataContext = viewModel;
        }

        private void Log_EntryAdded(object sender, EntryAddedEventArgs e)
        {
            this.Log.ScrollIntoView(e.Entry);
        }
    }
}
