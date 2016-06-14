namespace SQLiteDebugger
{
    using System;
    using System.Collections.Concurrent;
    using System.Runtime.InteropServices;
    using System.Text;
    using System.Threading;

    public class StatementInterceptor
    {
        private DebugServer server;

        private SQLiteEntryPoint connect;
        private SQLiteTraceV2 trace;
        private SQLiteRow row;
        private SQLiteProfile profile;

        private ConcurrentDictionary<IntPtr, int> queries = new ConcurrentDictionary<IntPtr, int>();
        private int currentId = 0;

        public StatementInterceptor(DebugServer server)
        {
            if (server == null)
            {
                throw new ArgumentNullException("server");
            }

            this.server = server;
            this.connect = this.Connect;
            this.trace = this.Trace;
            this.row = this.Row;
            this.profile = this.Profile;

            if (UnsafeNativeMethods.sqlite3_auto_extension(this.connect) != UnsafeNativeMethods.SQLITE_OK)
            {
                throw new InvalidOperationException("SQLite could not register extension");
            }
        }

        private int Connect(IntPtr db, ref string errMsg, IntPtr api)
        {
            UnsafeNativeMethods.sqlite3_trace_v2(db, this.trace, IntPtr.Zero);
            UnsafeNativeMethods.sqlite3_row(db, this.row, IntPtr.Zero);
            UnsafeNativeMethods.sqlite3_profile(db, this.profile, IntPtr.Zero);
            return UnsafeNativeMethods.SQLITE_OK;
        }

        private void Trace(IntPtr data, IntPtr stmt, string sql)
        {
            var id = this.queries.GetOrAdd(stmt, (k) => Interlocked.Increment(ref this.currentId));

            this.server.SendTrace(sql, id);

            var count = UnsafeNativeMethods.sqlite3_column_count(stmt);
            for (var i = 0; i < count; i++)
            {
                UnsafeNativeMethods.sqlite3_column_name(stmt, i);
            }
        }

        private void Row(IntPtr data, IntPtr stmt)
        {
            var count = UnsafeNativeMethods.sqlite3_column_count(stmt);
            for (var i = 0; i < count; i++)
            {
                UnsafeNativeMethods.sqlite3_column_text(stmt, i);
            }
        }

        private void Profile(IntPtr data, IntPtr stmt, ulong time)
        {
            int id;
            this.queries.TryRemove(stmt, out id);

            this.server.SendProfile(id, TimeSpan.FromMilliseconds(time / 1000000));
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
