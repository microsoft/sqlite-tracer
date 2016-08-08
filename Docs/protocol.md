# Network Protocol

The library and log viewer communicate over a TCP socket via JSON. The protocol is documented here to enable porting the library from C# for use in applications written in other languages.

The library opens a TCP listening socket on a port specified by the target application. The log viewer connects to this port, and both endpoints send messages as events occur- there is no synchronous conversation between the two.

A single message consists of a 32-bit, little-endian integer specifying the message payload length, followed by that many bytes of UTF-8 encoded text (the payload length does not include the integer itself). The text represents a single JSON object.

Each message object contains a `Type` field containing a string value specifying the object's type. Fields can contain integers, strings, bools, DateTime values encoded as ISO-8601 strings, and TimeSpan values encoded as `[d.]hh:mm:ss[.fffffff]` strings. Each endpoint has its own set of message types to send:

## Messages from the library to the log viewer

### `"log"`

```json
{ "Type": "log", "Time": <DateTime>, "Message": <string> }
```

The `log` message contains an arbitrary string from the target application.

### `"open"`

```json
{ "Type": "open", "Id": <int>, "Filename": <string> }
```

The `open` message indicates a new database connection in the target application. The connection is given a numerical ID, unique among connections in that run of the application.

The message also contains the path to the connection's main database file.

### `"close"`

```json
{ "Type": "close", "Id": <int> }
```

The `close` message indicates that a previously-opened connection has been closed.

### `"trace"`

```json
{
	"Type": "trace", "Time": <DateTime>, "Id": <int>, "Connection": <int>,
	"Query": <string>, "Plan": <string>
}
```

The `trace` message indicates that a statement has begun execution. The statement is given a numerical ID, unique among statements in that run of the applictaion.

The message also contains the statement's start time, the ID of the connection it is running on, and the query text with parameters expanded.

If enabled by the log viewer, the message also contains a string representation of the query plan, provided by SQLite's `EXPLAIN QUERY PLAN` - each result row on a line, indented by its `order` column.

### `"profile"`

```json
{
	"Type": "profile", "Time": <DateTime>, "Id": <int>, "Duration": <TimeSpan>,
	"Results": [
		{ "column1": "value1", "column2": "value2", "column3": "value3" },
		{ "column1": "value1", "column2": "value2", "column3": "value3" },
	]
}
```

The `profile` message indicates that a statement has completed execution.

The message contains the ID of the completed statement and its duration as reported by SQLite.

If enabled by the log viewer, the message also contains an array of objects representing the query's result rows.

## Messages from the log viewer to the library

### `"options"`

```json
{ "Type": "options", "Plan": <bool>, "Results": <bool>, "Pause": <bool> }
```

The `options` message controls the behavior of the library in the target application. The log viewer typically sends an `options` message immediately after the connection is established.

The `Plan` field determines whether query plans are collected and sent in `trace` messages. The `Results` field determines whether query results are collected and sent in `profile` messages. The `Pause` field determines whether the library waits for a `debug` message with the step `Action` after each `trace` message.

### `"debug"`

```json
{ "Type": "debug", "Action": <int> }
```

The `debug` message represents a command to the library. Currently, the `Action` field must be set to `0`, which triggers a single-statement step when the application is paused.
