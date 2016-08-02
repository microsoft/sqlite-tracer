namespace SQLiteDebugger
{
    using Newtonsoft.Json;
    using System;

    public class LogMessage
    {
        [JsonProperty]
        public const string Type = "log";

        public DateTime Time { get; set; }

        public string Message { get; set; }
    }
}
