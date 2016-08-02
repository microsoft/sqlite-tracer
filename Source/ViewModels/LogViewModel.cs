// -----------------------------------------------------------------------
// <copyright file="LogViewModel.cs" company="Microsoft">
//     Copyright (c) Microsoft Corporation. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

namespace SQLiteLogViewer.ViewModels
{
    using Models;
    using SQLiteDebugger;
    using System;
    using System.Collections.Generic;
    using System.Collections.ObjectModel;
    using System.Collections.Specialized;
    using System.ComponentModel;
    using System.Data;
    using System.Linq;
    using System.Windows;
    using Toolkit;

    public class LogViewModel : ObservableObject, IDisposable
    {
        private EventAggregator events;
        private readonly DebugClient client;

        private bool disposed = false;
        private readonly Log log;

        private bool collectPlan = false;
        private bool collectResults = false;
        private bool pause = false;

        private EntryViewModel selectedEntry;
        private Dictionary<int, EntryViewModel> pendingEntries = new Dictionary<int, EntryViewModel>();

        private FilterViewModel selectedFilter;

        private ConnectionViewModel selectedConnection;
        private Dictionary<int, string> connections = new Dictionary<int, string>();
        private HashSet<string> databases = new HashSet<string>();

        public LogViewModel(EventAggregator events, DebugClient client, string filename = null)
        {
            if (events == null)
            {
                throw new ArgumentNullException("events");
            }

            this.events = events;

            if (filename != null)
            {
                this.log = new Log(events, filename);
            }
            else
            {
                this.log = new Log(events);
            }

            this.Step = new DelegateCommand(() =>
            {
                this.client.SendStep();
            });

            this.Query = new DelegateCommand(() => this.Conductor.OpenQueryWindow());

            this.Replay = new DelegateCommand(
                () =>
                {
                    var entry = this.SelectedEntry;
                    this.client.SendQuery(entry.Connection, entry.Filepath, entry.Text);
                },
                () =>
                {
                    var entry = this.SelectedEntry;
                    return !this.Pause && entry != null && entry.Type == EntryType.Query;
                });

            this.Exec = new DelegateCommand(
                () =>
                {
                    var connection = this.SelectedConnection;
                    this.client.SendQuery(connection.Id, connection.Filename, this.QueryText);
                },
                () =>
                {
                    var connection = this.SelectedConnection;
                    return !this.Pause && connection != null;
                });

            this.Entries = new VirtualLog(events, this.log);
            this.Entries.CollectionChanged += this.Entries_CollectionChanged;

            this.NewFilter = new DelegateCommand(() =>
            {
                var filter = new FilterViewModel { Parent = this };
                this.Filters.Add(filter);
                this.SelectedFilter = filter;

                this.Conductor.OpenFilterWindow();
            });
            this.EditFilter = new DelegateCommand(() => this.Conductor.OpenFilterWindow());
            this.DeleteFilter = new DelegateCommand(() => this.Filters.Remove(this.SelectedFilter));

            this.client = client;
            events.Subscribe<ConnectEvent>(this.ConnectionOpened, ThreadAffinity.PublisherThread);

            events.Subscribe<OpenMessage>(this.OpenReceived, ThreadAffinity.UIThread);
            events.Subscribe<CloseMessage>(this.CloseReceived, ThreadAffinity.UIThread);

            events.Subscribe<LogMessage>(this.LogReceived, ThreadAffinity.UIThread);
            events.Subscribe<TraceMessage>(this.TraceReceived, ThreadAffinity.UIThread);
            events.Subscribe<ProfileMessage>(this.ProfileReceived, ThreadAffinity.UIThread);
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
                // EventAggregator's Publish can sometimes race with our Unsubscribe calls here
                // thus, event handlers that use unmanaged resources must check this flag
                this.disposed = true;
                this.Entries.Dispose();
                this.log.Dispose();
            }
        }

        public VirtualLog Entries { get; private set; }

        public EntryViewModel SelectedEntry
        {
            get { return this.selectedEntry; }
            set { this.SetField(ref this.selectedEntry, value); }
        }

        public ObservableCollection<FilterViewModel> Filters
        {
            get { return this.Entries.Filters; }
        }

        public FilterViewModel SelectedFilter
        {
            get { return this.selectedFilter; }
            set { this.SetField(ref this.selectedFilter, value); }
        }

        public CommandBase NewFilter { get; private set; }

        public CommandBase EditFilter { get; private set; }

        public CommandBase DeleteFilter { get; private set; }

        public bool CollectPlan
        {
            get { return this.collectPlan; }
            set { this.SetOption(ref this.collectPlan, value, "CollectPlan"); }
        }

        public bool CollectResults
        {
            get { return this.collectResults; }
            set { this.SetOption(ref this.collectResults, value, "CollectResults"); }
        }

        public bool Pause
        {
            get { return this.pause; }
            set { this.SetOption(ref this.pause, value, "Pause"); }
        }

        public CommandBase Step { get; private set; }

        private void SetOption(ref bool option, bool value, string name)
        {
            if (option == value)
            {
                return;
            }

            option = value;
            this.SendOptions();

            this.NotifyPropertyChanged(name);
        }

        private void SendOptions()
        {
            this.client.SendOptions(this.CollectPlan, this.CollectResults, this.Pause);
        }

        public IConductor Conductor { get; set; }

        public CommandBase Query { get; private set; }

        public CommandBase Replay { get; private set; }

        public IEnumerable<ConnectionViewModel> Connections
        {
            get
            {
                return this.connections.Select((entry) => new ConnectionViewModel()
                {
                    Id = entry.Key,
                    Filename = entry.Value
                }).Concat(this.databases.Select((filename) => new ConnectionViewModel()
                {
                    Id = 0,
                    Filename = filename
                }));
            }
        }

        public ConnectionViewModel SelectedConnection
        {
            get
            {
                return this.selectedConnection;
            }

            set
            {
                if (this.selectedConnection != value)
                {
                    this.selectedConnection = value;
                    this.NotifyPropertyChanged("SelectedConnection");
                }
            }
        }

        public string QueryText { get; set; }

        public CommandBase Exec { get; private set; }

        public bool IsDirty { get; set; }

        public void SaveToFile(string filename)
        {
            this.log.SaveToFile(filename);
            this.IsDirty = false;
        }

        private void Entries_CollectionChanged(object sender, NotifyCollectionChangedEventArgs e)
        {
            switch (e.Action)
            {
                case NotifyCollectionChangedAction.Add:
                    this.IsDirty = true;

                    foreach (var entry in e.NewItems.Cast<EntryViewModel>())
                    {
                        this.events.Publish<EntryViewModel>(entry);
                    }

                    break;

                case NotifyCollectionChangedAction.Reset:
                    break;

                default:
                    throw new InvalidOperationException("Log can only be appended to.");
            }
        }

        private void ConnectionOpened(ConnectEvent c)
        {
            this.SendOptions();
        }

        private void LogReceived(LogMessage message)
        {
            if (this.disposed)
            {
                return;
            }

            var entry = new Entry
            {
                Type = EntryType.Message,
                Start = message.Time,
                End = message.Time,
                Text = message.Message
            };

            this.log.AddEntry(entry);
        }

        private void OpenReceived(OpenMessage open)
        {
            this.connections.Add(open.Id, open.Filename);
            this.NotifyPropertyChanged("Connections");
        }

        private void CloseReceived(CloseMessage close)
        {
            string database = null;
            this.connections.TryGetValue(close.Id, out database);
            if (database == null)
            {
                return;
            }

            this.databases.Add(database);
            this.connections.Remove(close.Id);
            this.NotifyPropertyChanged("Connections");
        }

        private void TraceReceived(TraceMessage trace)
        {
            if (this.disposed)
            {
                return;
            }

            string database = string.Empty;
            this.connections.TryGetValue(trace.Connection, out database);

            var entry = new Entry
            {
                Type = EntryType.Query,
                Connection = trace.Connection,
                Database = database,
                Start = trace.Time,
                Text = trace.Query,
                Plan = trace.Plan
            };

            this.log.AddEntry(entry);
            this.pendingEntries.Add(trace.Id, this.Entries.Last());
        }

        private void ProfileReceived(ProfileMessage profile)
        {
            if (this.disposed)
            {
                return;
            }

            EntryViewModel entry = null;
            this.pendingEntries.TryGetValue(profile.Id, out entry);
            if (entry == null)
            {
                return;
            }

            this.pendingEntries.Remove(profile.Id);

            entry.End = entry.Start + profile.Duration;
            entry.Results = profile.Results != null ? profile.Results.AsDataView() : null;
        }
    }
}
