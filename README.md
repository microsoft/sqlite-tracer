# SQLite Tracer

SQLite Tracer is a library and tool to profile and debug applications using SQLite. The library is linked into the target application, where it acts as a server for the tool to connect and receive information on statement execution. 

The main features are:

 * Intercept statements executed by the target application
   * Log query start time, end time, and duration
   * Capture query results
   * Capture query plans
 * Execute statements in the target application's connections
   * Replay previously-intercepted queries
   * Write new statements
 * Pause the target application and step execution one statement at a time
 * Filter, sort, and save intercepted logs

## Building SQLite Tracer

The provided solution `SQLiteTracer.sln` builds both the debugger assembly `SQLiteDebugger` and the log viewer app `SQLiteLogViewer`. The debug assembly depends on `Newtonsoft.Json`, and the log viewer also depends on `System.Data.SQLite` (currently only for SQLite itself, not the ADO.NET provider). These dependencies are provided with NuGet, and can be retrieved in Visual Studio with the "Restore NuGet packages" command.

The debug assembly is also provided as a NuGet package: `SQLiteDebugger`. This package can be built by running the following command from the `Source/SQLiteDebugger` directory, after building the library in release mode:

```batchfile
nuget pack SQLiteDebugger.csproj -Prop Configuration=Release -IncludeReferencedProjects
```

## Integrating the library with an application

To integrate the library into a target application, the app must link to the `SQLiteDebugger` assembly and then create a `DebugServer` object. Calling the `DebugServer.Listen` method will start a new task to listen for the log viewer, as well as hook into SQLite to begin capturing events.

The library assembly itself does not reference any particular SQLite bindings, though it does expect the target application's native SQLite DLL to be named `SQLite.Interop.dll` (as in `System.Data.SQLite`), so it can be used with any C# application that includes a native SQLite library with that name. It requires SQLite version `3.14.0` or later, which is included in `System.Data.SQLite` version `1.0.103.0`.

The `DebugServer` class implements the `IDebugTraceSender` interface, to enable the application to send any of its own logs to SQLite Tracer's log viewer.

## Running the log viewer

### Startup

By default, the log viewer attempts to connect to `localhost:3000`. The port can be changed by passing the new value as a command line argument.

### Capture settings

By default, SQLite tracer will capture log messages and queries without their plans or results. The check boxes in the upper-right enable capturing this data at a slight performance cost to the target application. Query plans and results are displayed in their own tabs in the bottom pane, and are only available for queries intercepted while capture was enabled.

### Statement execution and replay

The Replay and Query buttons attempt to execute SQLite statements in the target application. The Replay button uses the currently-selected query, while the Query button opens a dialog for specifying connection and statement text.

If the target connection is still open, the statement will be executed on that connection, enabling it to access transaction state, temporary tables, and attached databases. If the target connection has closed, the statement will be executed on the same database file, and thus potentially fail.

### Application pause and step

The Pause button allows the log viewer to freeze the application when it next encounters a SQLite statement. The Step button allows the application to execute one statement and then to freeze on the next. The Continue button reverts the application to its normal mode of execution.

Unfortunately, because of SQLite's locking scheme, the Replay and Query buttons described above can only be used while the application is running (at least according to the library; debuggers still work here), as attemting to execute them in a paused application would lead to deadlock.

### Sorting and filtering

Logs can be sorted by clicking on the column headers. The sort order cycles through none, ascending, and descending. Multiple columns can be sorted hierarchically by holding the shift key.

Logs can be filtered using the right pane. Each filter represents the logical AND-ing of its non-default values, and all enabled filters are logically OR-ed together. The resulting condition is applied to determine which log entries are visible.
