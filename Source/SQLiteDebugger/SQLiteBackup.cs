namespace SQLiteDebugger
{
    using System;
    using System.Collections.Generic;
    using System.Diagnostics.CodeAnalysis;
    using System.Linq;
    using System.Text;
    using System.Threading.Tasks;

    public class SQLiteBackup : IDisposable
    {
        [SuppressMessage("Microsoft.Reliability", "CA2006:UseSafeHandleToEncapsulateNativeResources", Justification = "This is SafeHandle-like")]
        private IntPtr backup;

        public SQLiteBackup(SQLiteConnection dest, string destName, SQLiteConnection source, string sourceName)
        {
            if (dest == null)
            {
                throw new ArgumentNullException("dest");
            }

            if (destName == null)
            {
                throw new ArgumentNullException("destName");
            }

            if (source == null)
            {
                throw new ArgumentNullException("source");
            }

            if (sourceName == null)
            {
                throw new ArgumentNullException("sourceName");
            }

            this.backup = UnsafeNativeMethods.sqlite3_backup_init(dest.Connection, destName, source.Connection, sourceName);
            if (this.backup == IntPtr.Zero)
            {
                throw new InvalidOperationException("SQLite could not start a backup");
            }
        }

        ~SQLiteBackup()
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
            UnsafeNativeMethods.sqlite3_backup_finish(this.backup);
        }

        public bool Step(int pages = -1)
        {
            var rc = UnsafeNativeMethods.sqlite3_backup_step(this.backup, pages);
            switch (rc)
            {
            case UnsafeNativeMethods.SQLITE_OK:
                return true;
            case UnsafeNativeMethods.SQLITE_DONE:
                return false;
            default:
                throw new InvalidOperationException("SQLite could not back up the database");
            }
        }
    }
}
