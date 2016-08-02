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
    using System.Diagnostics.CodeAnalysis;
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

        public LogViewModel(EventAggregator events, int port)
        {
            if (events == null)
            {
                throw new ArgumentNullException("events");
            }

            this.events = events;

            this.Entries = new ObservableCollection<EntryViewModel>();
            this.log.Entries.CollectionChanged += this.Log_CollectionChanged;

            this.client = new DebugClient(events);

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
                Start = message.Time,
                End = message.Time,
                Text = message.Message
            };

            this.log.Entries.Add(entry);
        }

        private void TraceReceived(TraceMessage trace)
        {
            var entry = new Entry { ID = trace.Id, Start = trace.Time, Text = trace.Query, Plan = trace.Plan };

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
