namespace SQLiteDebugger
{
    using System;
    using System.Collections.Generic;
    using System.Diagnostics.CodeAnalysis;
    using System.Linq;
    using System.Text;
    using System.Threading.Tasks;

    public class SQLiteConnection : IDisposable
    {
        [SuppressMessage("Microsoft.Reliability", "CA2006:UseSafeHandleToEncapsulateNativeResources", Justification = "This is SafeHandle-like")]
        private IntPtr connection;

        public SQLiteConnection(string filename, bool create = false)
        {
            var flags = UnsafeNativeMethods.SQLITE_OPEN_READWRITE;
            if (create)
            {
                flags |= UnsafeNativeMethods.SQLITE_OPEN_CREATE;
            }

            var rc = UnsafeNativeMethods.sqlite3_open_v2(filename, out this.connection, flags, null);
            if (rc != UnsafeNativeMethods.SQLITE_OK)
            {
                throw new InvalidOperationException("SQLite could not create a database");
            }
        }

        ~SQLiteConnection()
        {
            this.Dispose(false);
        }

        public void Dispose()
        {
            this.Dispose(true);
            GC.SuppressFinalize(this);
        }

        [SuppressMessage("Microsoft.Usage", "CA1806:DoNotIgnoreMethodResults", Justification = "Disposing of object")]
        protected virtual void Dispose(bool disposing)
        {
            UnsafeNativeMethods.sqlite3_close_v2(this.connection);
        }

        public SQLiteStatement Prepare(string sql)
        {
            IntPtr stmt;
            var rc = UnsafeNativeMethods.sqlite3_prepare_v2(this.connection, sql, -1, out stmt, IntPtr.Zero);
            if (rc != UnsafeNativeMethods.SQLITE_OK)
            {
                var error = StatementInterceptor.UTF8ToString(UnsafeNativeMethods.sqlite3_errmsg(this.connection));
                throw new ArgumentException(error, sql);
            }

            return new SQLiteStatement(stmt);
        }

        public void Exec(string sql)
        {
            var rc = UnsafeNativeMethods.sqlite3_exec(this.connection, sql, null, IntPtr.Zero, IntPtr.Zero);
            if (rc != UnsafeNativeMethods.SQLITE_OK)
            {
                var error = StatementInterceptor.UTF8ToString(UnsafeNativeMethods.sqlite3_errmsg(this.connection));
                throw new ArgumentException(error, sql);
            }
        }
    }
}
