namespace SQLiteDebugger
{
    using Newtonsoft.Json;

    public class OptionsMessage
    {
        [JsonProperty]
        public const string Type = "options";

        public bool Plan { get; set; }

        public bool Results { get; set; }

        public bool Pause { get; set; }
    }
}
