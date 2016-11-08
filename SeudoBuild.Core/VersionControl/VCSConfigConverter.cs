using System;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace SeudoBuild.VCS
{
    public class VCSConfigConverter : JsonConverter
    {
        public override bool CanConvert(Type objectType)
        {
            return (objectType == typeof(VCSConfig));
        }

        public override object ReadJson(JsonReader reader, Type objectType, object existingValue, JsonSerializer serializer)
        {
            JObject jobj = JObject.Load(reader);
            if (jobj["Type"].Value<string>() == "Git")
                return jobj.ToObject<Git.GitVCSConfig>(serializer);

            return null;
        }

        public override bool CanWrite
        {
            get { return false; }
        }

        public override void WriteJson(JsonWriter writer, object value, JsonSerializer serializer)
        {
            throw new NotImplementedException();
        }
    }
}
