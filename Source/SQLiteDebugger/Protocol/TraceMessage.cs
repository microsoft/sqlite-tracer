namespace SQLiteDebugger
{
    using Newtonsoft.Json;
    using System;

    public class TraceMessage
    {
        [JsonProperty]
        public const string Type = "trace";

        public DateTime Time { get; set; }

        public int Id { get; set; }

        public int Database { get; set; }

        public string Query { get; set; }

        public string Plan { get; set; }
    }
}
