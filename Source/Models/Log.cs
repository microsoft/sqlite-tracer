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
    using System.Linq;
    using System.Text;
    using System.Diagnostics.CodeAnalysis;

    public class Log : IDisposable
    {
        private const string Columns = "rowid, type, connection, database, " +
                                       "strftime('%Y-%m-%d %H:%M:%f', start), " +
                                       "strftime('%Y-%m-%d %H:%M:%f', end), " +
                                       "text, plan, results";

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

            this.select = this.connection.Prepare(string.Format(
                CultureInfo.InvariantCulture,
                @"SELECT {0} FROM entries ORDER BY rowid LIMIT ? OFFSET ?",
                Columns));

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
                this.select.Dispose();
                this.count.Dispose();
                this.insert.Dispose();
                this.updateEnd.Dispose();
                this.updateResults.Dispose();

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

        public IEnumerable<Entry> GetEntries(int offset, int length, ISet<Filter> filters)
        {
            if (filters == null || filters.Count == 0)
            {
                foreach (var entry in this.GetEntries(offset, length, this.select, 0))
                {
                    yield return entry;
                }

                yield break;
            }

            int bindOffset;
            var sql = SelectFilter(Columns, filters, out bindOffset) + " LIMIT ? OFFSET ?";

            using (var query = this.connection.Prepare(sql))
            {
                Bind(query, filters);

                // can't just return, or `using` will end before the first iteration
                foreach (var entry in this.GetEntries(offset, length, query, bindOffset))
                {
                    yield return entry;
                }
            }
        }

        private IEnumerable<Entry> GetEntries(int offset, int length, SQLiteStatement query, int bindOffset)
        {
            query.Bind(bindOffset + 1, length);
            query.Bind(bindOffset + 2, offset);

            while (query.Step())
            {
                var id = query.ColumnInt(0);
                var type = (EntryType)query.ColumnInt(1);
                var connectionId = query.ColumnInt(2);
                var database = query.ColumnText(3);
                var start = query.ColumnText(4);
                var end = query.ColumnText(5);
                var text = query.ColumnText(6);
                var plan = query.ColumnText(7);
                var results = query.ColumnText(8);

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

            query.Reset();
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

        public int CountFilter(ISet<Filter> filters)
        {
            if (filters == null || filters.Count == 0)
            {
                return this.Count;
            }

            int bindOffset;
            var sql = SelectFilter("COUNT(*)", filters, out bindOffset);

            using (var query = this.connection.Prepare(sql))
            {
                BindFilter(query, filters);

                query.Step();
                return query.ColumnInt(0);
            }
        }

        private static string SelectFilter(string columns, ISet<Filter> filters, out int bindOffset)
        {
            if (filters == null || filters.Count == 0)
            {
                throw new ArgumentException("Filters cannot be empty", "filters");
            }

            var conditions = new List<string>(filters.Count);
            var condition = new List<string>(5);

            bindOffset = 0;
            foreach (var filter in filters)
            {
                condition.Clear();

                if (filter.Type != null)
                {
                    var invert = filter.Invert.HasFlag(FilterField.Type);
                    condition.Add(string.Format(CultureInfo.InvariantCulture, "type {0} ?", invert ? "!=" : "="));
                    bindOffset += 1;
                }

                if (!string.IsNullOrEmpty(filter.Database))
                {
                    var invert = filter.Invert.HasFlag(FilterField.Database);
                    condition.Add(string.Format(CultureInfo.InvariantCulture, "database {0} ?", invert ? "NOT LIKE" : "LIKE"));
                    bindOffset += 1;
                }

                if (filter.Complete != null)
                {
                    var invert = filter.Complete.Value != filter.Invert.HasFlag(FilterField.Complete);
                    condition.Add(string.Format(CultureInfo.InvariantCulture, "end {0} ?", invert ? "!=" : "="));
                    bindOffset += 1;
                }

                if (!string.IsNullOrEmpty(filter.Text))
                {
                    var invert = filter.Invert.HasFlag(FilterField.Text);
                    condition.Add(string.Format(CultureInfo.InvariantCulture, "text {0} ?", invert ? "NOT LIKE" : "LIKE"));
                    bindOffset += 1;
                }

                if (!string.IsNullOrEmpty(filter.Plan))
                {
                    var invert = filter.Invert.HasFlag(FilterField.Plan);
                    condition.Add(string.Format(CultureInfo.InvariantCulture, "plan {0} ?", invert ? "NOT LIKE" : "LIKE"));
                    bindOffset += 1;
                }

                conditions.Add(string.Join(" AND ", condition));
            }

            var template = @"SELECT {0} FROM entries WHERE {1} ORDER BY rowid";
            return string.Format(CultureInfo.InvariantCulture, template, columns, string.Join(" OR ", conditions));
        }

        [SuppressMessage("Microsoft.Globalization", "CA1303:Do not pass literals as localized parameters", Justification = "SQL string")]
        private static void BindFilter(SQLiteStatement query, ISet<Filter> filters)
        {
            var i = 1;
            foreach (var filter in filters)
            {
                if (filter.Type != null)
                {
                    query.Bind(i++, (int)filter.Type);
                }

                if (filter.Database != null)
                {
                    query.Bind(i++, string.Format(CultureInfo.InvariantCulture, "%{0}%", filter.Database));
                }

                if (filter.Complete != null)
                {
                    query.Bind(i++, default(DateTime).ToString("o", CultureInfo.InvariantCulture));
                }

                if (filter.Text != null)
                {
                    query.Bind(i++, string.Format(CultureInfo.InvariantCulture, "%{0}%", filter.Text));
                }

                if (filter.Plan != null)
                {
                    query.Bind(i++, string.Format(CultureInfo.InvariantCulture, "%{0}%", filter.Plan));
                }
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
