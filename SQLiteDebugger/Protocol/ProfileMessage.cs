namespace SQLiteDebugger
{
    using Newtonsoft.Json;
    using Newtonsoft.Json.Linq;
    using System;
    using System.Data;

    public class ProfileMessage
    {
        public string Database { get; set; }

        public int Id { get; set; }

        public DateTime Time { get; set; }

        public TimeSpan Duration { get; set; }

        public DataTable Results { get; set; }
    }
}
