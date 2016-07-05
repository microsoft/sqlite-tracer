namespace SQLiteLogViewer
{
    using SQLiteDebugger;
    using System;
    using System.Diagnostics.CodeAnalysis;
    using Toolkit;
    using ViewModels;

    public class MainViewModel : ObservableObject, IDisposable
    {
        private LogViewModel logViewModel;

        [SuppressMessage("Microsoft.Reliability", "CA2000:Dispose objects before losing scope", Justification = "Disposed of in containing class")]
        public MainViewModel(EventAggregator events, DebugClient client)
        {
            this.New = new DelegateCommand(() =>
            {
                this.LogViewModel.Dispose();
                this.LogViewModel = new LogViewModel(events, client) { Conductor = this.Conductor };
            });

            this.Open = new DelegateCommand(() =>
            {
            });

            this.Save = new DelegateCommand(() =>
            {
            });
        }

        public void Dispose()
        {
            this.Dispose(true);
            GC.SuppressFinalize(this);
        }

        protected virtual void Dispose(bool disposing)
        {
            if (disposing)
            {
                if (this.LogViewModel != null)
                {
                    this.LogViewModel.Dispose();
                }
            }
        }

        public IConductor Conductor { get; set; }

        public LogViewModel LogViewModel
        {
            get { return this.logViewModel; }

            set
            {
                this.logViewModel = value;
                this.NotifyPropertyChanged("LogViewModel");
            }
        }

        public CommandBase New { get; private set; }

        public CommandBase Open { get; private set; }

        public CommandBase Save { get; private set; }
    }
}
