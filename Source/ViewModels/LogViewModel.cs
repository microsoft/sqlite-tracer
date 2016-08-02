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

    public class LogViewModel : ObservableObject
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
        private DelegateCommand query;
        private DelegateCommand replay;

        private ConnectionViewModel selectedConnection;
        private HashSet<string> databases = new HashSet<string>();
        private DelegateCommand exec;

        public LogViewModel(EventAggregator events, int port)
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

            this.query = new DelegateCommand(() => this.Conductor.OpenQueryWindow());

            this.replay = new DelegateCommand(
                () =>
                {
                    var entry = this.SelectedEntry;
                    this.client.SendQuery(entry.Database, entry.Filepath, entry.Text);
                },
                () =>
                {
                    var entry = this.SelectedEntry;
                    return !this.Pause && entry != null && entry.Type == EntryType.Query;
                });

            this.exec = new DelegateCommand(
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

            this.client = new DebugClient(events);

            events.Subscribe<OpenMessage>(this.OpenReceived, ThreadAffinity.UIThread);
            events.Subscribe<CloseMessage>(this.CloseReceived, ThreadAffinity.UIThread);

            events.Subscribe<ConnectEvent>((c) => this.SendOptions(), ThreadAffinity.PublisherThread);
            events.Subscribe<LogMessage>(this.LogReceived, ThreadAffinity.UIThread);
            events.Subscribe<TraceMessage>(this.TraceReceived, ThreadAffinity.UIThread);
            events.Subscribe<ProfileMessage>(this.ProfileReceived, ThreadAffinity.UIThread);

            this.client.Connect("localhost", port);
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

        public CommandBase Query
        {
            get { return this.query; }
        }

        public CommandBase Replay
        {
            get { return this.replay; }
        }

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

        public CommandBase Exec
        {
            get { return this.exec; }
        }

        private void Log_CollectionChanged(object sender, NotifyCollectionChangedEventArgs e)
        {
            switch (e.Action)
            {
                case NotifyCollectionChangedAction.Add:
                    var entry = new EntryViewModel(e.NewItems.Cast<Entry>().Single());
                    this.Entries.Insert(e.NewStartingIndex, entry);
                    this.events.Publish<EntryViewModel>(entry);

                    break;

                case NotifyCollectionChangedAction.Move:
                    this.Entries.Move(e.OldStartingIndex, e.NewStartingIndex);
                    break;

                case NotifyCollectionChangedAction.Remove:
                    this.Entries.RemoveAt(e.OldStartingIndex);
                    break;

                case NotifyCollectionChangedAction.Replace:
                    this.Entries[e.OldStartingIndex] = new EntryViewModel(e.NewItems.Cast<Entry>().Single());
                    break;

                case NotifyCollectionChangedAction.Reset:
                    this.Entries = new ObservableCollection<EntryViewModel>(
                        e.NewItems.Cast<Entry>().Select(model => new EntryViewModel(model)));
                    break;
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
                ID = trace.Id,
                Database = trace.Connection,
                Filename = this.connections[trace.Connection],
                Start = trace.Time,
                Text = trace.Query,
                Plan = trace.Plan
            };

            this.log.Entries.Add(entry);
            this.pendingEntries.Add(entry.ID, this.Entries.Last());
        }

        private void ProfileReceived(ProfileMessage profile)
        {
            EntryViewModel entry = this.pendingEntries[profile.Id];
            this.pendingEntries.Remove(entry.ID);

            entry.End = entry.Start + profile.Duration;
            entry.Results = profile.Results != null ? profile.Results.AsDataView() : null;
        }
    }
}
