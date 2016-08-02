namespace SQLiteDebugger
{
    using Newtonsoft.Json;
    using System;

    public class LogMessage
    {
        [JsonProperty]
        public const string Type = "log";

        public string Database { get; set; }

        public DateTime Time { get; set; }

        public string Message { get; set; }
    }
}
