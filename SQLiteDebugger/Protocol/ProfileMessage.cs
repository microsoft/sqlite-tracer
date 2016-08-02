namespace SQLiteDebugger
{
    using Newtonsoft.Json;
    using System;
    using System.Data;

    public class ProfileMessage
    {
        [JsonProperty]
        public const string Type = "profile";

        public string Database { get; set; }

        public int Id { get; set; }

        public DateTime Time { get; set; }

        public TimeSpan Duration { get; set; }

        public DataTable Results { get; set; }
    }
}
