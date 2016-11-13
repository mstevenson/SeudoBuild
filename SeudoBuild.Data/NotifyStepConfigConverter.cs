using System;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace SeudoBuild
{
    public class NotifyStepConfigConverter : JsonConverter
    {
        public override bool CanConvert(Type objectType)
        {
            return (objectType == typeof(NotifyStepConfig));
        }

        public override object ReadJson(JsonReader reader, Type objectType, object existingValue, JsonSerializer serializer)
        {
            //JObject jobj = JObject.Load(reader);
            //if (jobj["Type"].Value<string>() == "Email Notification")
            //    return jobj.ToObject<EmailNotifyConfig>(serializer);
            
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
