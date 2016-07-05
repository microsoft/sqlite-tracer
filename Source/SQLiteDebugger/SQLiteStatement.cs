namespace SQLiteDebugger
{
    using System;
    using System.Diagnostics.CodeAnalysis;

    public class SQLiteStatement : IDisposable
    {
        private IntPtr stmt;

        internal SQLiteStatement(IntPtr stmt)
        {
            this.stmt = stmt;
        }

        ~SQLiteStatement()
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
            UnsafeNativeMethods.sqlite3_finalize(this.stmt);
        }

        public void Reset()
        {
            var rc = UnsafeNativeMethods.sqlite3_reset(this.stmt);
            if (rc != UnsafeNativeMethods.SQLITE_OK)
            {
                throw new InvalidOperationException("SQLite could not reset the statement");
            }
        }

        public void Bind(int index, string text)
        {
            if (text == null)
            {
                this.Bind(index);
                return;
            }

            var rc = UnsafeNativeMethods.sqlite3_bind_text(this.stmt, index, text, -1, new IntPtr(-1));
            if (rc != UnsafeNativeMethods.SQLITE_OK)
            {
                throw new InvalidOperationException("SQLite could not bind the parameter");
            }
        }

        public void Bind(int index)
        {
            var rc = UnsafeNativeMethods.sqlite3_bind_null(this.stmt, index); 
            if (rc != UnsafeNativeMethods.SQLITE_OK)
            {
                throw new InvalidOperationException("SQLite could not bind the parameter");
            }
        }

        public void Step()
        {
            var rc = UnsafeNativeMethods.sqlite3_step(this.stmt);
            if (rc != UnsafeNativeMethods.SQLITE_DONE && rc != UnsafeNativeMethods.SQLITE_OK)
            {
                throw new InvalidOperationException("SQLite could not run the query");
            }
        }
    }
}