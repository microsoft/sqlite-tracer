namespace SQLiteDebugger
{
    using System;
    using System.Collections.Concurrent;
    using System.Collections.Generic;
    using System.Data;
    using System.Diagnostics;
    using System.Diagnostics.CodeAnalysis;
    using System.Globalization;
    using System.Linq;
    using System.Runtime.InteropServices;
    using System.Text;
    using System.Threading;

    public class StatementInterceptor
    {
        private DebugServer server;

        private SQLiteEntryPoint connectHandler;
        private SQLiteClose closeHandler;
        private SQLiteTraceV2 traceHandler;
        private SQLiteRow rowHandler;
        private SQLiteProfile profileHandler;

        private int currentDbId = 0;
        private ConcurrentDictionary<IntPtr, int> connections = new ConcurrentDictionary<IntPtr, int>();

        private int currentStmtId = 0;
        private ConcurrentDictionary<IntPtr, int> queries = new ConcurrentDictionary<IntPtr, int>();

        private bool collectResults = false;
        private ConcurrentDictionary<IntPtr, DataTable> results = new ConcurrentDictionary<IntPtr, DataTable>();

        private bool pause = false;
        private bool step = false;
        private object pauseMonitor = new object();

        public StatementInterceptor(DebugServer server)
        {
            if (server == null)
            {
                throw new ArgumentNullException("server");
            }

            this.server = server;
            this.connectHandler = this.OnConnect;
            this.closeHandler = this.OnClose;
            this.traceHandler = this.OnTrace;
            this.rowHandler = this.OnRow;
            this.profileHandler = this.OnProfile;

            if (UnsafeNativeMethods.sqlite3_auto_extension(this.connectHandler) != UnsafeNativeMethods.SQLITE_OK)
            {
                throw new InvalidOperationException("SQLite could not register extension");
            }
        }

        public bool CollectPlan { get; set; }

        public bool CollectResults
        {
            get
            {
                return this.collectResults;
            }

            set
            {
                if (this.collectResults == value)
                {
                    return;
                }

                this.collectResults = value;
                if (this.collectResults)
                {
                    foreach (var db in this.connections.Keys)
                    {
                        UnsafeNativeMethods.sqlite3_row(db, this.rowHandler, IntPtr.Zero);
                    }
                }
                else
                {
                    foreach (var db in this.connections.Keys)
                    {
                        UnsafeNativeMethods.sqlite3_row(db, null, IntPtr.Zero);
                    }
                }
            }
        }

        public bool Pause
        {
            get
            {
                return this.pause;
            }

            set
            {
                lock (this.pauseMonitor)
                {
                    this.pause = value;
                    Monitor.Pulse(this.pauseMonitor);
                }
            }
        }

        public void Step()
        {
            lock (this.pauseMonitor)
            {
                this.step = true;
                Monitor.Pulse(this.pauseMonitor);
            }
        }

        [SuppressMessage("Microsoft.Globalization", "CA1303:Do not pass literals as localized parameters", Justification = "Debug text")]
        public void Exec(int connectionId, string filename, string sql)
        {
            if (sql == null)
            {
                throw new ArgumentNullException("sql");
            }

            var dispose = false;
            int rc;

            var connection = this.connections.FirstOrDefault((c) => c.Value == connectionId).Key;
            if (connection == IntPtr.Zero)
            {
                dispose = true;

                var flags = UnsafeNativeMethods.SQLITE_OPEN_READWRITE;
                rc = UnsafeNativeMethods.sqlite3_open_v2(filename, out connection, flags, null);
                if (rc != UnsafeNativeMethods.SQLITE_OK)
                {
                    this.server.SendLog(string.Format(CultureInfo.InvariantCulture, "[Error] Could not open database {0}", filename));
                    return;
                }
            }

            rc = UnsafeNativeMethods.sqlite3_exec(connection, sql, null, IntPtr.Zero, IntPtr.Zero);
            if (rc != UnsafeNativeMethods.SQLITE_OK)
            {
                var error = UTF8ToString(UnsafeNativeMethods.sqlite3_errmsg(connection));
                var message = string.Format(
                    CultureInfo.InvariantCulture,
                    "[Error] Could not execute statement '{0}'\n{1}",
                    sql.Replace(Environment.NewLine, " ").Replace('\n', ' '),
                    error);
                this.server.SendLog(message);
            }

            if (dispose)
            {
                rc = UnsafeNativeMethods.sqlite3_close_v2(connection);
                if (rc != UnsafeNativeMethods.SQLITE_OK)
                {
                    throw new InvalidOperationException("SQLite could not close the connection");
                }
            }
        }

        private int OnConnect(IntPtr db, ref string errMsg, IntPtr api)
        {
            var id = Interlocked.Increment(ref this.currentDbId);
            this.connections.TryAdd(db, id);
            UnsafeNativeMethods.sqlite3_close_handler(db, this.closeHandler, IntPtr.Zero);

            UnsafeNativeMethods.sqlite3_trace_v2(db, this.traceHandler, IntPtr.Zero);
            UnsafeNativeMethods.sqlite3_profile(db, this.profileHandler, IntPtr.Zero);

            if (this.collectResults)
            {
                UnsafeNativeMethods.sqlite3_row(db, this.rowHandler, IntPtr.Zero);
            }

            var path = UTF8ToString(UnsafeNativeMethods.sqlite3_db_filename(db, "main"));
            this.server.SendOpen(id, path);

            return UnsafeNativeMethods.SQLITE_OK;
        }

        private void OnClose(IntPtr data, IntPtr db)
        {
            int id;
            if (!this.connections.TryRemove(db, out id))
            {
                return;
            }

            this.server.SendClose(id);
        }

        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Reliability", "CA2000:Dispose objects before losing scope", Justification = "Object is stored in hash table")]
        private void OnTrace(IntPtr data, IntPtr stmt, string sql)
        {
            // don't go into an endless loop trying to EXPLAIN EXPLAIN EXPLAIN EXPLAIN ...
            if (sql.StartsWith("EXPLAIN", StringComparison.Ordinal))
            {
                return;
            }

            var id = this.queries.GetOrAdd(stmt, (k) => Interlocked.Increment(ref this.currentStmtId));

            string plan = null;
            if (this.CollectPlan)
            {
                var lines = ExplainQueryPlan(stmt).Select(qpr => new string('\t', qpr.Order) + qpr.Detail);
                plan = string.Join("\n", lines);
            }

            int db = 0;
            this.connections.TryGetValue(UnsafeNativeMethods.sqlite3_db_handle(stmt), out db);
            this.server.SendTrace(id, db, sql, plan);

            if (this.CollectResults)
            {
                var count = UnsafeNativeMethods.sqlite3_column_count(stmt);
                var dataTable = new DataTable() { Locale = CultureInfo.InvariantCulture };

                for (var i = 0; i < count; i++)
                {
                    dataTable.Columns.Add(UTF8ToString(UnsafeNativeMethods.sqlite3_column_name(stmt, i)));
                }

                this.results.TryAdd(stmt, dataTable);
            }

            lock (this.pauseMonitor)
            {
                while (this.Pause && !this.step)
                {
                    Monitor.Wait(this.pauseMonitor);
                }

                this.step = false;
            }
        }

        private void OnRow(IntPtr data, IntPtr stmt)
        {
            DataTable dataTable;
            if (!this.results.TryGetValue(stmt, out dataTable))
            {
                return;
            }

            var count = dataTable.Columns.Count;
            var row = dataTable.NewRow();

            for (var i = 0; i < count; i++)
            {
                row.SetField(i, UTF8ToString(UnsafeNativeMethods.sqlite3_column_text(stmt, i)));
            }

            dataTable.Rows.Add(row);
        }

        private void OnProfile(IntPtr data, IntPtr stmt, ulong time)
        {
            int id;
            if (!this.queries.TryRemove(stmt, out id))
            {
                return;
            }

            DataTable dataTable;
            this.results.TryRemove(stmt, out dataTable);

            this.server.SendProfile(id, TimeSpan.FromMilliseconds(time / 1000000), dataTable);
        }

        private struct QueryPlanRow
        {
            public int SelectId { get; set; }

            public int Order { get; set; }

            public int From { get; set; }

            public string Detail { get; set; }
        }

        private static IEnumerable<QueryPlanRow> ExplainQueryPlan(IntPtr stmt)
        {
            string explainSql = string.Format("EXPLAIN QUERY PLAN {0}", UTF8ToString(UnsafeNativeMethods.sqlite3_sql(stmt)));

            IntPtr db = UnsafeNativeMethods.sqlite3_db_handle(stmt);
            IntPtr explainStmt;
            int rc = UnsafeNativeMethods.sqlite3_prepare_v2(db, explainSql, -1, out explainStmt, IntPtr.Zero);
            if (rc != UnsafeNativeMethods.SQLITE_OK)
            {
                yield return new QueryPlanRow { Detail = "[Error] Could not get query plan" };
                yield break;
            }

            while (UnsafeNativeMethods.sqlite3_step(explainStmt) == UnsafeNativeMethods.SQLITE_ROW)
            {
                int selectId = UnsafeNativeMethods.sqlite3_column_int(explainStmt, 0);
                int order = UnsafeNativeMethods.sqlite3_column_int(explainStmt, 1);
                int from = UnsafeNativeMethods.sqlite3_column_int(explainStmt, 2);
                string detail = UTF8ToString(UnsafeNativeMethods.sqlite3_column_text(explainStmt, 3));

                yield return new QueryPlanRow { SelectId = selectId, Order = order, From = from, Detail = detail };
            }

            UnsafeNativeMethods.sqlite3_finalize(explainStmt);
        }

        internal static string UTF8ToString(IntPtr data)
        {
            if (data == IntPtr.Zero)
            {
                return string.Empty;
            }

            var length = 0;
            while (Marshal.ReadByte(data, length) != 0)
            {
                length++;
            }

            if (length == 0)
            {
                return string.Empty;
            }

            byte[] buffer = new byte[length];
            Marshal.Copy(data, buffer, 0, length);

            return Encoding.UTF8.GetString(buffer, 0, length);
        }
    }
}
