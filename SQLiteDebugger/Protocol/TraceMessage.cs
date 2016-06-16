namespace SQLiteDebugger
{
    using Newtonsoft.Json;
    using Newtonsoft.Json.Linq;
    using System;
    using System.Data;

    public class TraceMessage
    {
        public string Database { get; set; }

        public int Id { get; set; }

        public DateTime Time { get; set; }

        public string Query { get; set; }

        public string Plan { get; set; }
    }
}
