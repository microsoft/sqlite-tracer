// -----------------------------------------------------------------------
// <copyright file="Log.cs" company="Microsoft">
//     Copyright (c) Microsoft Corporation. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

namespace SQLiteLogViewer.Models
{
    using SQLiteDebugger;
    using System;
    using System.Collections.ObjectModel;
    using System.Collections.Specialized;
    using System.Data;
    using System.Globalization;
    using System.Linq;

    public class Log : IDisposable
    {
        private SQLiteConnection connection;
        private SQLiteStatement insert;

        public Log()
        {
            this.connection = new SQLiteConnection(":memory:", true);
            this.CreateDatabase();
            this.insert = this.connection.Prepare(
                "INSERT INTO entries (start, end, text, plan) VALUES (?, ?, ?, ?)");

            this.Entries = new ObservableCollection<Entry>();
            this.Entries.CollectionChanged += this.Entries_CollectionChanged;
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
                this.insert.Dispose();
                this.connection.Dispose();
            }
        }

        public ObservableCollection<Entry> Entries { get; private set; }

        private void CreateDatabase()
        {
            this.connection.Exec(@"CREATE TABLE entries (
                id INTEGER PRIMARY KEY ASC,
                start TEXT,
                end TEXT,
                text TEXT,
                plan TEXT
            )");
        }

        private void Entries_CollectionChanged(object sender, NotifyCollectionChangedEventArgs e)
        {
            switch (e.Action)
            {
                case NotifyCollectionChangedAction.Add:
                    foreach (var entry in e.NewItems.Cast<Entry>())
                    {
                        this.insert.Bind(1, entry.Start.ToString("o", CultureInfo.InvariantCulture));
                        this.insert.Bind(2, entry.End.ToString("o", CultureInfo.InvariantCulture));
                        this.insert.Bind(3, entry.Text);
                        this.insert.Bind(4, entry.Plan);
                        this.insert.Step();
                        this.insert.Reset();
                    }

                    break;

                default:
                    throw new InvalidOperationException("Log can only be appended to.");
            }
        }
    }
}
