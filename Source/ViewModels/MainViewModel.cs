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
        private string logPath;

        [SuppressMessage("Microsoft.Reliability", "CA2000:Dispose objects before losing scope", Justification = "Disposed of in containing class")]
        public MainViewModel(EventAggregator events, DebugClient client)
        {
            this.New = new DelegateCommand(() =>
            {
                if (this.LogViewModel.IsDirty)
                {
                    var confirmation = this.Conductor.ConfirmSave();
                    if (confirmation == null)
                    {
                        return;
                    }
                    else if (confirmation == true)
                    {
                        this.Save.Execute(null);
                    }
                }

                this.logPath = null;
                this.LogViewModel.Dispose();
                this.LogViewModel = new LogViewModel(events, client) { Conductor = this.Conductor };
            });

            this.Open = new DelegateCommand(() =>
            {
                if (this.LogViewModel.IsDirty)
                {
                    var confirmation = this.Conductor.ConfirmSave();
                    if (confirmation == null)
                    {
                        return;
                    }
                    else if (confirmation == true)
                    {
                        this.Save.Execute(null);
                    }
                }

                var path = this.Conductor.OpenOpenFileDialog();
                if (path == null)
                {
                    return;
                }

                this.logPath = path;
                this.LogViewModel.Dispose();
                this.LogViewModel = new LogViewModel(events, client, this.logPath) { Conductor = this.Conductor };
            });

            this.Save = new DelegateCommand(() =>
            {
                if (this.logPath == null)
                {
                    this.logPath = this.Conductor.OpenSaveFileDialog();
                    if (this.logPath == null)
                    {
                        return;
                    }
                }

                this.LogViewModel.SaveToFile(this.logPath);
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
            get
            {
                return this.logViewModel;
            }

            set
            {
                this.logViewModel = value;
                this.NotifyPropertyChanged("LogViewModel");
            }
        }

        public CommandBase New { get; private set; }

        public CommandBase Open { get; private set; }

        public CommandBase Save { get; private set; }

        public bool Cleanup()
        {
            if (this.LogViewModel.IsDirty)
            {
                var confirmation = this.Conductor.ConfirmSave();
                if (confirmation == null)
                {
                    return false;
                }
                else if (confirmation == true)
                {
                    this.Save.Execute(null);
                    return !this.LogViewModel.IsDirty;
                }
            }

            return true;
        }
    }
}
