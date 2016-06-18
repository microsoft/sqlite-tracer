namespace SQLiteDebugger
{
    using Newtonsoft.Json;

    public class OpenMessage
    {
        [JsonProperty]
        public const string Type = "open";

        public int Database { get; set; }

        public string Filename { get; set; }
    }
}
