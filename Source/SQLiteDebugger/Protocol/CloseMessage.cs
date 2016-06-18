namespace SQLiteDebugger
{
    using Newtonsoft.Json;

    public class CloseMessage
    {
        [JsonProperty]
        public const string Type = "close";

        public int Id { get; set; }
    }
}
