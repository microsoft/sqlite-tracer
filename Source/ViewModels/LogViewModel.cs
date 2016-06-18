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
        private ReplayCommand replay;

        public LogViewModel(EventAggregator events, int port)
        {
            if (events == null)
            {
                throw new ArgumentNullException("events");
            }

            this.events = events;

            this.replay = new ReplayCommand(this);

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
            set { this.SetOption(ref this.collectPlan, value); }
        }

        public bool CollectResults
        {
            get { return this.collectResults; }
            set { this.SetOption(ref this.collectResults, value); }
        }

        public bool Pause
        {
            get { return this.pause; }
            set { this.SetOption(ref this.pause, value); }
        }

        private void SetOption(ref bool option, bool value)
        {
            if (option == value)
            {
                return;
            }

            option = value;
            this.SendOptions();
        }

        private void SendOptions()
        {
            this.client.SendOptions(this.collectPlan, this.collectResults, false);
        }

        public CommandBase Replay
        {
            get { return this.replay; }
        }

        private class ReplayCommand : CommandBase
        {
            private LogViewModel owner;

            public ReplayCommand(LogViewModel owner)
            {
                this.owner = owner;
            }

            public override bool CanExecute(object parameter)
            {
                var entry = parameter as EntryViewModel;
                if (entry == null)
                {
                    return false;
                }

                if (entry.Type != EntryType.Query)
                {
                    return false;
                }

                return true;
            }

            public override void Execute(object parameter)
            {
                if (!this.CanExecute(parameter))
                {
                    throw new ArgumentException("Selected item is not a query", "parameter");
                }

                var entry = parameter as EntryViewModel;
                this.owner.client.SendQuery(entry.Database, entry.Filepath, entry.Text);
            }
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
        }

        private void CloseReceived(CloseMessage close)
        {
            this.connections.Remove(close.Id);
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
