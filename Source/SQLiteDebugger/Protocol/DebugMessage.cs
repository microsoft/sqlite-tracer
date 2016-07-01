namespace SQLiteDebugger
{
    using Newtonsoft.Json;

    public class DebugMessage
    {
        [JsonProperty]
        public const string Type = "debug";

        public DebugAction Action { get; set; }
    }

    public enum DebugAction
    {
        Step,
    }
}
