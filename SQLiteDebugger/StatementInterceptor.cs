namespace SQLiteDebugger
{
    using System;
    using System.Collections.Concurrent;
    using System.Collections.Generic;
    using System.Data;
    using System.Globalization;
    using System.Linq;
    using System.Runtime.InteropServices;
    using System.Text;
    using System.Threading;

    public class StatementInterceptor
    {
        private DebugServer server;

        private SQLiteEntryPoint connectHandler;
        private SQLiteTraceV2 traceHandler;
        private SQLiteRow rowHandler;
        private SQLiteProfile profileHandler;

        private bool collectResults = false;
        private List<IntPtr> dbs = new List<IntPtr>();

        private int currentId = 0;
        private ConcurrentDictionary<IntPtr, int> queries = new ConcurrentDictionary<IntPtr, int>();

        private ConcurrentDictionary<IntPtr, DataTable> results = new ConcurrentDictionary<IntPtr, DataTable>();

        public StatementInterceptor(DebugServer server)
        {
            if (server == null)
            {
                throw new ArgumentNullException("server");
            }

            this.server = server;
            this.connectHandler = this.OnConnect;
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
                    foreach (var db in this.dbs)
                    {
                        UnsafeNativeMethods.sqlite3_row(db, this.rowHandler, IntPtr.Zero);
                    }
                }
                else
                {
                    foreach (var db in this.dbs)
                    {
                        UnsafeNativeMethods.sqlite3_row(db, null, IntPtr.Zero);
                    }
                }
            }
        }

        private int OnConnect(IntPtr db, ref string errMsg, IntPtr api)
        {
            UnsafeNativeMethods.sqlite3_trace_v2(db, this.traceHandler, IntPtr.Zero);
            UnsafeNativeMethods.sqlite3_profile(db, this.profileHandler, IntPtr.Zero);

            if (this.collectResults)
            {
                UnsafeNativeMethods.sqlite3_row(db, this.rowHandler, IntPtr.Zero);
            }

            return UnsafeNativeMethods.SQLITE_OK;
        }

        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Reliability", "CA2000:Dispose objects before losing scope", Justification = "Object is stored in hash table")]
        private void OnTrace(IntPtr data, IntPtr stmt, string sql)
        {
            // don't go into an endless loop trying to EXPLAIN EXPLAIN EXPLAIN EXPLAIN ...
            if (sql.StartsWith("EXPLAIN", StringComparison.Ordinal))
            {
                return;
            }

            var id = this.queries.GetOrAdd(stmt, (k) => Interlocked.Increment(ref this.currentId));

            string plan = null;
            if (this.CollectPlan)
            {
                var lines = ExplainQueryPlan(stmt).Select(qpr => new string('\t', qpr.Order) + qpr.Detail);
                plan = string.Join("\n", lines);
            }

            this.server.SendTrace(id, sql, plan);

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
