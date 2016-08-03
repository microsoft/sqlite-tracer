namespace SQLiteDebugger
{
    using System;
    using System.Diagnostics.CodeAnalysis;
    using System.Runtime.InteropServices;

    #region SQLite declarations (not generated code but marked as such for StyleCop)

    internal delegate int SQLiteEntryPoint(IntPtr db, ref string errMsg, IntPtr api);

    internal delegate int SQLiteExec(IntPtr data, int columns, string[] names, string[] values);

    internal delegate void SQLiteClose(IntPtr data, IntPtr db);

    internal delegate void SQLiteTrace(uint type, IntPtr data, IntPtr stmt, IntPtr arg);

    internal delegate void SQLiteRow(IntPtr data, IntPtr stmt);

    internal delegate void SQLiteProfile(IntPtr data, IntPtr stmt, ulong time);

    internal static class UnsafeNativeMethods
    {
        internal const int SQLITE_OK = 0;
        internal const int SQLITE_ROW = 100;
        internal const int SQLITE_DONE = 101;

        internal const int SQLITE_OPEN_READONLY = 1;
        internal const int SQLITE_OPEN_READWRITE = 2;
        internal const int SQLITE_OPEN_CREATE = 4;

        internal const uint SQLITE_TRACE_STMT = 1;
        internal const uint SQLITE_TRACE_PROFILE = 2;
        internal const uint SQLITE_TRACE_ROW = 4;
        internal const uint SQLITE_TRACE_CLOSE = 8;

        [DllImport("SQLite.Interop.dll", CallingConvention = CallingConvention.Cdecl)]
        internal static extern IntPtr sqlite3_free(IntPtr data);

        [DllImport("SQLite.Interop.dll", CallingConvention = CallingConvention.Cdecl)]
        internal static extern int sqlite3_auto_extension(SQLiteEntryPoint entryPoint);

        [SuppressMessage("Microsoft.Globalization", "CA2101:SpecifyMarshalingForPInvokeStringArguments", Justification = "Technically should be UTF-8")]
        [DllImport("SQLite.Interop.dll", CallingConvention = CallingConvention.Cdecl)]
        internal static extern int sqlite3_open_v2(string filename, out IntPtr db, int flags, string vfs);

        [DllImport("SQLite.Interop.dll", CallingConvention = CallingConvention.Cdecl)]
        internal static extern int sqlite3_close_v2(IntPtr db);

        [DllImport("SQLite.Interop.dll", CallingConvention = CallingConvention.Cdecl)]
        internal static extern IntPtr sqlite3_errmsg(IntPtr db);

        [SuppressMessage("Microsoft.Globalization", "CA2101:SpecifyMarshalingForPInvokeStringArguments", Justification = "Technically should be UTF-8")]
        [DllImport("SQLite.Interop.dll", CallingConvention = CallingConvention.Cdecl)]
        internal static extern IntPtr sqlite3_db_filename(IntPtr db, string name);

        [DllImport("SQLite.Interop.dll", CallingConvention = CallingConvention.Cdecl)]
        internal static extern IntPtr sqlite3_trace_v2(IntPtr db, uint flags, SQLiteTrace trace, IntPtr data);

        [SuppressMessage("Microsoft.Globalization", "CA2101:SpecifyMarshalingForPInvokeStringArguments", Justification = "Technically should be UTF-8")]
        [DllImport("SQLite.Interop.dll", CallingConvention = CallingConvention.Cdecl)]
        internal static extern int sqlite3_exec(IntPtr db, string sql, SQLiteExec callback, IntPtr data, IntPtr err);

        [SuppressMessage("Microsoft.Globalization", "CA2101:SpecifyMarshalingForPInvokeStringArguments", Justification = "Technically should be UTF-8")]
        [DllImport("SQLite.Interop.dll", CallingConvention = CallingConvention.Cdecl)]
        internal static extern int sqlite3_prepare_v2(IntPtr db, string sql, int len, out IntPtr stmt, IntPtr tail);

        [DllImport("SQLite.Interop.dll", CallingConvention = CallingConvention.Cdecl)]
        internal static extern long sqlite3_last_insert_rowid(IntPtr db);

        [DllImport("SQLite.Interop.dll", CallingConvention = CallingConvention.Cdecl)]
        internal static extern int sqlite3_finalize(IntPtr stmt);

        [DllImport("SQLite.Interop.dll", CallingConvention = CallingConvention.Cdecl)]
        internal static extern int sqlite3_reset(IntPtr stmt);

        [DllImport("SQLite.Interop.dll", CallingConvention = CallingConvention.Cdecl)]
        internal static extern int sqlite3_bind_int(IntPtr stmt, int index, int number);

        [SuppressMessage("Microsoft.Globalization", "CA2101:SpecifyMarshalingForPInvokeStringArguments", Justification = "Technically should be UTF-8")]
        [DllImport("SQLite.Interop.dll", CallingConvention = CallingConvention.Cdecl)]
        internal static extern int sqlite3_bind_text(IntPtr stmt, int index, string text, int length, IntPtr destructor);

        [DllImport("SQLite.Interop.dll", CallingConvention = CallingConvention.Cdecl)]
        internal static extern int sqlite3_bind_null(IntPtr stmt, int index);

        [DllImport("SQLite.Interop.dll", CallingConvention = CallingConvention.Cdecl)]
        internal static extern int sqlite3_step(IntPtr stmt);

        [DllImport("SQLite.Interop.dll", CallingConvention = CallingConvention.Cdecl)]
        internal static extern IntPtr sqlite3_expanded_sql(IntPtr stmt);

        [DllImport("SQLite.Interop.dll", CallingConvention = CallingConvention.Cdecl)]
        internal static extern IntPtr sqlite3_db_handle(IntPtr stmt);

        [DllImport("SQLite.Interop.dll", CallingConvention = CallingConvention.Cdecl)]
        internal static extern int sqlite3_column_count(IntPtr stmt);

        [DllImport("SQLite.Interop.dll", CallingConvention = CallingConvention.Cdecl)]
        internal static extern IntPtr sqlite3_column_name(IntPtr stmt, int N);

        [DllImport("SQLite.Interop.dll", CallingConvention = CallingConvention.Cdecl)]
        internal static extern int sqlite3_column_int(IntPtr stmt, int N);

        [DllImport("SQLite.Interop.dll", CallingConvention = CallingConvention.Cdecl)]
        internal static extern IntPtr sqlite3_column_text(IntPtr stmt, int N);

        [DllImport("SQLite.Interop.dll", CallingConvention = CallingConvention.Cdecl)]
        internal static extern IntPtr sqlite3_sql(IntPtr stmt);

        [SuppressMessage("Microsoft.Globalization", "CA2101:SpecifyMarshalingForPInvokeStringArguments", Justification = "Technically should be UTF-8")]
        [DllImport("SQLite.Interop.dll", CallingConvention = CallingConvention.Cdecl)]
        internal static extern IntPtr sqlite3_backup_init(IntPtr dest, string destName, IntPtr source, string sourceName);

        [DllImport("SQLite.Interop.dll", CallingConvention = CallingConvention.Cdecl)]
        internal static extern int sqlite3_backup_step(IntPtr backup, int pages);

        [DllImport("SQLite.Interop.dll", CallingConvention = CallingConvention.Cdecl)]
        internal static extern int sqlite3_backup_finish(IntPtr backup);
    }

    #endregion
}
