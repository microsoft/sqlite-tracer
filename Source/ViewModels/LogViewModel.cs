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
    using System.Data;
    using System.Linq;
    using System.Windows;
    using Toolkit;

    public class LogViewModel : ObservableObject, IDisposable
    {
        private EventAggregator events;
        private readonly DebugClient client;

        private readonly Log log = new Log();

        private bool collectPlan = false;
        private bool collectResults = false;
        private bool pause = false;

        private EntryViewModel selectedEntry;
        private Dictionary<int, EntryViewModel> pendingEntries = new Dictionary<int, EntryViewModel>();

        private Dictionary<int, string> connections = new Dictionary<int, string>();

        private ConnectionViewModel selectedConnection;
        private HashSet<string> databases = new HashSet<string>();

        public LogViewModel(EventAggregator events, DebugClient client)
        {
            if (events == null)
            {
                throw new ArgumentNullException("events");
            }

            this.events = events;

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

            this.Entries = new ObservableCollection<EntryViewModel>();
            this.log.Entries.CollectionChanged += this.Log_CollectionChanged;

            this.client = client;
            events.Subscribe<ConnectEvent>((c) => this.SendOptions(), ThreadAffinity.PublisherThread);

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
                this.log.Dispose();
            }
        }

        public ObservableCollection<EntryViewModel> Entries { get; private set; }

        public EntryViewModel SelectedEntry
        {
            get
            {
                return this.selectedEntry;
            }

            set
            {
                this.selectedEntry = value;
                this.NotifyPropertyChanged("SelectedEntry");
            }
        }

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

        public void LoadFromFile(string filename)
        {
            this.log.LoadFromFile(filename);
            this.IsDirty = false;
        }

        private void Log_CollectionChanged(object sender, NotifyCollectionChangedEventArgs e)
        {
            switch (e.Action)
            {
                case NotifyCollectionChangedAction.Add:
                    foreach (var entry in e.NewItems.Cast<Entry>())
                    {
                        var entryViewModel = new EntryViewModel(entry);
                        this.Entries.Insert(e.NewStartingIndex, entryViewModel);
                        this.events.Publish<EntryViewModel>(entryViewModel);
                    }

                    this.IsDirty = true;
                    break;

                default:
                    throw new InvalidOperationException("Log can only be appended to.");
            }
        }

        private void LogReceived(LogMessage message)
        {
            var entry = new Entry
            {
                Type = EntryType.Message,
                Start = message.Time,
                End = message.Time,
                Text = message.Message
            };

            this.log.Entries.Add(entry);
        }

        private void OpenReceived(OpenMessage open)
        {
            this.connections.Add(open.Id, open.Filename);
            this.NotifyPropertyChanged("Connections");
        }

        private void CloseReceived(CloseMessage close)
        {
            this.databases.Add(this.connections[close.Id]);
            this.connections.Remove(close.Id);
            this.NotifyPropertyChanged("Connections");
        }

        private void TraceReceived(TraceMessage trace)
        {
            var entry = new Entry
            {
                Type = EntryType.Query,
                Connection = trace.Connection,
                Database = this.connections[trace.Connection],
                Start = trace.Time,
                Text = trace.Query,
                Plan = trace.Plan
            };

            this.log.Entries.Add(entry);
            this.pendingEntries.Add(trace.Id, this.Entries.Last());
        }

        private void ProfileReceived(ProfileMessage profile)
        {
            var entry = this.pendingEntries[profile.Id];
            this.pendingEntries.Remove(profile.Id);

            entry.End = entry.Start + profile.Duration;
            entry.Results = profile.Results != null ? profile.Results.AsDataView() : null;
        }
    }
}
