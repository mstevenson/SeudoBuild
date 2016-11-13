using System;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace SeudoBuild.Modules.ZipArchive
{
    public class FolderArchiveConfigConverter : JsonConverter
    {
        public override bool CanConvert(Type objectType)
        {
            return (objectType == typeof(ArchiveStepConfig));
        }

        public override object ReadJson(JsonReader reader, Type objectType, object existingValue, JsonSerializer serializer)
        {
            JObject jobj = JObject.Load(reader);
            if (jobj["Type"].Value<string>() == "Folder")
            {
                return jobj.ToObject<FolderArchiveConfig>(serializer);
            }
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
