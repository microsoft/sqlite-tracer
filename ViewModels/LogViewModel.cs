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
    using System.Net.Sockets;
    using System.Threading.Tasks;
    using System.Windows;
    using System.Windows.Input;

    public class LogViewModel : ObservableObject
    {
        private readonly DebugClient client;

        private readonly Log log = new Log();

        private bool collectPlan = false;
        private bool collectResults = false;
        private bool pause = false;

        private EntryViewModel selectedEntry;
        private Dictionary<int, EntryViewModel> pendingEntries = new Dictionary<int, EntryViewModel>();

        public LogViewModel(int port)
        {
            if (port < 0)
            {
                throw new ArgumentOutOfRangeException("port");
            }

            this.Entries = new ObservableCollection<EntryViewModel>();
            this.log.Entries.CollectionChanged += this.Log_CollectionChanged;

            this.client = new DebugClient();
            this.client.Connected += (sender, e) => this.SendOptions();
            this.client.Connect("localhost", port);

            this.client.LogReceived += this.Client_LogReceived;
            this.client.TraceReceived += this.Client_TraceReceived;
            this.client.ProfileReceived += this.Client_ProfileReceived;
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

        public event EventHandler<EntryAddedEventArgs> EntryAdded;

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
                    var handler = this.EntryAdded;
                    if (handler != null)
                    {
                        handler(this, new EntryAddedEventArgs { Entry = entry });
                    }

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

        private void Client_LogReceived(object sender, LogEventArgs e)
        {
            var message = e.Message;
            var entry = new Entry
            {
                Start = message.Time,
                End = message.Time,
                Text = message.Message
            };

            Application.Current.Dispatcher.Invoke(() => { this.log.Entries.Add(entry); });
        }

        private void Client_TraceReceived(object sender, TraceEventArgs e)
        {
            var trace = e.Message;
            var entry = new Entry { ID = trace.Id, Start = trace.Time, Text = trace.Query, Plan = trace.Plan };

            Application.Current.Dispatcher.Invoke(() =>
            {
                this.log.Entries.Add(entry);
                this.pendingEntries.Add(entry.ID, this.Entries.Last());
            });
        }

        private void Client_ProfileReceived(object sender, ProfileEventArgs e)
        {
            var profile = e.Message;
            EntryViewModel entry = this.pendingEntries[profile.Id];
            this.pendingEntries.Remove(entry.ID);

            Application.Current.Dispatcher.Invoke(() =>
            {
                entry.End = entry.Start + profile.Duration;
                entry.Results = profile.Results != null ? profile.Results.AsDataView() : null;
            });
        }
    }

    [SuppressMessage("Microsoft.StyleCop.CSharp.MaintainabilityRules", "SA1402:FileMayOnlyContainASingleClass", Justification = "EventArgs")]
    public class EntryAddedEventArgs : EventArgs
    {
        public EntryViewModel Entry { get; set; }
    }
}
