namespace SQLiteDebugger
{
    using Newtonsoft.Json;
    using Newtonsoft.Json.Linq;
    using System;
    using System.Data;

    public class LogMessage
    {
        public string Database { get; set; }

        public DateTime Time { get; set; }

        public string Message { get; set; }
    }
}
