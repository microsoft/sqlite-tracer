namespace SQLiteDebugger
{
    using System;
    using System.Diagnostics.CodeAnalysis;
    using System.Runtime.InteropServices;

    #region SQLite declarations (not generated code but marked as such for StyleCop)

    internal delegate int SQLiteEntryPoint(IntPtr db, ref string errMsg, IntPtr api);

	internal delegate int SQLiteExec(IntPtr data, int columns, string[] names, string[] values);

    internal delegate void SQLiteClose(IntPtr data, IntPtr db);

    internal delegate void SQLiteTraceV2(IntPtr data, IntPtr stmt, string query);

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

        [DllImport("SQLite.Interop.dll", CallingConvention = CallingConvention.Cdecl)]
        internal static extern int sqlite3_auto_extension(SQLiteEntryPoint entryPoint);

        [SuppressMessage("Microsoft.Globalization", "CA2101:SpecifyMarshalingForPInvokeStringArguments", Justification = "Technically should be UTF-8")]
        [DllImport("SQLite.Interop.dll", CallingConvention = CallingConvention.Cdecl)]
        internal static extern int sqlite3_open_v2(string filename, out IntPtr db, int flags, string vfs);

        [DllImport("SQLite.Interop.dll", CallingConvention = CallingConvention.Cdecl)]
        internal static extern int sqlite3_close_v2(IntPtr db);

        [DllImport("SQLite.Interop.dll", CallingConvention = CallingConvention.Cdecl)]
        internal static extern IntPtr sqlite3_errmsg(IntPtr db);

        [DllImport("SQLite.Interop.dll", CallingConvention = CallingConvention.Cdecl)]
        internal static extern IntPtr sqlite3_close_handler(IntPtr db, SQLiteClose close, IntPtr data);

        [SuppressMessage("Microsoft.Globalization", "CA2101:SpecifyMarshalingForPInvokeStringArguments", Justification = "Technically should be UTF-8")]
        [DllImport("SQLite.Interop.dll", CallingConvention = CallingConvention.Cdecl)]
        internal static extern IntPtr sqlite3_db_filename(IntPtr db, string name);

        [DllImport("SQLite.Interop.dll", CallingConvention = CallingConvention.Cdecl)]
        internal static extern IntPtr sqlite3_trace_v2(IntPtr db, SQLiteTraceV2 trace, IntPtr data);

        [DllImport("SQLite.Interop.dll", CallingConvention = CallingConvention.Cdecl)]
        internal static extern IntPtr sqlite3_row(IntPtr db, SQLiteRow trace, IntPtr data);

        [DllImport("SQLite.Interop.dll", CallingConvention = CallingConvention.Cdecl)]
        internal static extern IntPtr sqlite3_profile(IntPtr db, SQLiteProfile profile, IntPtr data);

        [SuppressMessage("Microsoft.Globalization", "CA2101:SpecifyMarshalingForPInvokeStringArguments", Justification = "Technically should be UTF-8")]
        [DllImport("SQLite.Interop.dll", CallingConvention = CallingConvention.Cdecl)]
        internal static extern int sqlite3_exec(IntPtr db, string sql, SQLiteExec callback, IntPtr data, IntPtr err);

        [SuppressMessage("Microsoft.Globalization", "CA2101:SpecifyMarshalingForPInvokeStringArguments", Justification = "Technically should be UTF-8")]
        [DllImport("SQLite.Interop.dll", CallingConvention = CallingConvention.Cdecl)]
        internal static extern int sqlite3_prepare_v2(IntPtr db, string sql, int len, out IntPtr stmt, IntPtr tail);

        [DllImport("SQLite.Interop.dll", CallingConvention = CallingConvention.Cdecl)]
        internal static extern int sqlite3_finalize(IntPtr stmt);

        [DllImport("SQLite.Interop.dll", CallingConvention = CallingConvention.Cdecl)]
        internal static extern int sqlite3_step(IntPtr stmt);

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
    }

    #endregion
}
