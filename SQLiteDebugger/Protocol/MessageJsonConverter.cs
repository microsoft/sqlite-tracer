namespace SQLiteDebugger
{
    using Newtonsoft.Json;
    using Newtonsoft.Json.Linq;
    using System;
    using System.Data;

    internal class MessageJsonConverter<T> : JsonConverter
    {
        private string typeName;

        public MessageJsonConverter(string typeName)
        {
            this.typeName = typeName;
        }

        public override bool CanConvert(Type objectType)
        {
            return typeof(T) == objectType;
        }

        public override bool CanRead
        {
            get { return false; }
        }

        public override object ReadJson(JsonReader reader, Type objectType, object existingValue, JsonSerializer serializer)
        {
            throw new NotImplementedException();
        }

        public override void WriteJson(JsonWriter writer, object value, JsonSerializer serializer)
        {
            var t = JToken.FromObject(value);

            if (t.Type == JTokenType.Object)
            {
                (t as JObject).AddFirst(new JProperty("type", this.typeName));
            }

            t.WriteTo(writer);
        }
    }
}
