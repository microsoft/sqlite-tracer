namespace SQLiteDebugger
{
    using Newtonsoft.Json;

    public class QueryMessage
    {
        [JsonProperty]
        public const string Type = "query";

        public int Connection { get; set; }

        public string Filename { get; set; }

        public string Query { get; set; }
    }
}
