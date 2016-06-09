namespace SQLiteDebugger
{
    using System;
    using System.Runtime.InteropServices;

    #region SQLite declarations (not generated code but marked as such for StyleCop)

    internal delegate int SQLiteEntryPoint(IntPtr db, ref string errMsg, IntPtr api);

    internal delegate void SQLiteTraceV2(IntPtr data, IntPtr stmt, string query);

    internal delegate void SQLiteProfile(IntPtr data, IntPtr stmt, ulong time);

    internal static class UnsafeNativeMethods
    {
        internal const int SQLITE_OK = 0;

        [DllImport("SQLite.Interop.dll", CallingConvention = CallingConvention.Cdecl)]
        internal static extern int sqlite3_auto_extension(SQLiteEntryPoint entryPoint);

        [DllImport("SQLite.Interop.dll", CallingConvention = CallingConvention.Cdecl)]
        internal static extern IntPtr sqlite3_trace_v2(IntPtr db, SQLiteTraceV2 trace, IntPtr data);

        [DllImport("SQLite.Interop.dll", CallingConvention = CallingConvention.Cdecl)]
        internal static extern IntPtr sqlite3_profile(IntPtr db, SQLiteProfile profile, IntPtr data);

        [DllImport("SQLite.Interop.dll", CallingConvention = CallingConvention.Cdecl)]
        internal static extern IntPtr sqlite3_sql(IntPtr stmt);
    }

    #endregion
}
