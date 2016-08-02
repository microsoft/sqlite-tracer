// -----------------------------------------------------------------------
// <copyright file="Log.cs" company="Microsoft">
//     Copyright (c) Microsoft Corporation. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

namespace SQLiteLogViewer.Models
{
    using Toolkit;
    using Newtonsoft.Json;
    using SQLiteDebugger;
    using System;
    using System.Collections.Generic;
    using System.Data;
    using System.Globalization;

    public class Log : IDisposable
    {
        private EventAggregator events;

        private SQLiteConnection connection;
        private SQLiteStatement select;
        private SQLiteStatement count;
        private SQLiteStatement insert;
        private SQLiteStatement updateEnd;
        private SQLiteStatement updateResults;

        public Log(EventAggregator events)
        {
            this.events = events;

            this.connection = new SQLiteConnection(string.Empty, true);
            this.CreateDatabase();

            this.select = this.connection.Prepare(
                @"SELECT rowid, type, connection, database,
                         strftime('%Y-%m-%d %H:%M:%f', start),
                         strftime('%Y-%m-%d %H:%M:%f', end),
                         text, plan, results
                    FROM entries ORDER BY rowid LIMIT ? OFFSET ?");

            this.count = this.connection.Prepare(@"SELECT COUNT(*) FROM entries");

            this.insert = this.connection.Prepare(@"INSERT INTO entries (
                type, connection, database, start, end, text, plan, results
            ) VALUES (?, ?, ?, julianday(?), julianday(?), ?, ?, ?)");

            this.updateEnd = this.connection.Prepare(
                "UPDATE entries SET end = julianday(?) WHERE rowid = ?");
            this.updateResults = this.connection.Prepare(
                "UPDATE entries SET results = ? WHERE rowid = ?");
        }

        public Log(EventAggregator events, string filename)
            : this(events)
        {
            using (var load = new SQLiteConnection(filename))
            using (var backup = new SQLiteBackup(this.connection, "main", load, "main"))
            {
                backup.Step();
            }
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

        public void AddEntry(Entry entry)
        {
            if (entry == null)
            {
                throw new ArgumentNullException("entry");
            }

            this.insert.Bind(1, (int)entry.Type);
            this.insert.Bind(2, entry.Connection);
            this.insert.Bind(3, entry.Database);
            this.insert.Bind(4, entry.Start.ToString("o", CultureInfo.InvariantCulture));
            this.insert.Bind(5, entry.End.ToString("o", CultureInfo.InvariantCulture));
            this.insert.Bind(6, entry.Text);
            this.insert.Bind(7, entry.Plan);
            this.insert.Bind(8, JsonConvert.SerializeObject(entry.Results));
            this.insert.Step();
            this.insert.Reset();

            entry.Id = (int)this.connection.LastInsertId();
            entry.Parent = this;

            this.events.Publish<Entry>(entry);
        }

        public IEnumerable<Entry> GetEntries(int offset, int length)
        {
            this.select.Bind(1, length);
            this.select.Bind(2, offset);

            while (this.select.Step())
            {
                var id = this.select.ColumnInt(0);
                var type = (EntryType)this.select.ColumnInt(1);
                var connectionId = this.select.ColumnInt(2);
                var database = this.select.ColumnText(3);
                var start = this.select.ColumnText(4);
                var end = this.select.ColumnText(5);
                var text = this.select.ColumnText(6);
                var plan = this.select.ColumnText(7);
                var results = this.select.ColumnText(8);

                var startDate = DateTime.Parse(start, null, DateTimeStyles.RoundtripKind);
                var endDate = DateTime.Parse(end, null, DateTimeStyles.RoundtripKind);

                var resultsTable = JsonConvert.DeserializeObject<DataTable>(results);

                yield return new Entry
                {
                    Id = id,
                    Parent = this,
                    Type = type,
                    Connection = connectionId,
                    Database = database,
                    Start = startDate,
                    End = endDate,
                    Text = text,
                    Plan = plan,
                    Results = resultsTable,
                };
            }

            this.select.Reset();
        }

        public int Count
        {
            get
            {
                this.count.Step();
                var n = this.count.ColumnInt(0);
                this.count.Reset();

                return n;
            }
        }

        public void SaveToFile(string filename)
        {
            using (var save = new SQLiteConnection(filename, true))
            using (var backup = new SQLiteBackup(save, "main", this.connection, "main"))
            {
                backup.Step();
            }
        }

        private void CreateDatabase()
        {
            this.connection.Exec(@"CREATE TABLE entries (
                rowid INTEGER PRIMARY KEY,
                type INTEGER,
                connection INTEGER,
                database TEXT,
                start REAL,
                end REAL,
                text TEXT,
                plan TEXT,
                results TEXT
            );

            CREATE INDEX entries_start ON entries (start);
            CREATE INDEX entries_end ON entries (end);
            CREATE INDEX entries_database ON entries (database);");
        }

        internal void UpdateEnd(int id, DateTime end)
        {
            this.updateEnd.Bind(1, end.ToString("o", CultureInfo.InvariantCulture));
            this.updateEnd.Bind(2, id);

            this.updateEnd.Step();
            this.updateEnd.Reset();
        }

        internal void UpdateResults(int id, DataTable results)
        {
            var resultsText = JsonConvert.SerializeObject(results);

            this.updateResults.Bind(1, resultsText);
            this.updateResults.Bind(2, id);

            this.updateResults.Step();
            this.updateResults.Reset();
        }
    }
}
