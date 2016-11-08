using System;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace SeudoBuild
{
    public class DistributeStepConfigConverter : JsonConverter
    {
        public override bool CanConvert(Type objectType)
        {
            return (objectType == typeof(DistributeStepConfig));
        }

        public override object ReadJson(JsonReader reader, Type objectType, object existingValue, JsonSerializer serializer)
        {
            JObject jobj = JObject.Load(reader);
            if (jobj["Type"].Value<string>() == "FTP Upload")
                return jobj.ToObject<FTPDistributeConfig>(serializer);

            if (jobj["Type"].Value<string>() == "SFTP Upload")
                return jobj.ToObject<SFTPDistributeConfig>(serializer);

            if (jobj["Type"].Value<string>() == "Steam Upload")
                return jobj.ToObject<SteamDistributeConfig>(serializer);
            
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
