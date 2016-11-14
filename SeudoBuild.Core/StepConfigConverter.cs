using System;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System.Collections.Generic;

namespace SeudoBuild
{
    // T should be a base type, like SourceStepConfig,
    // and U is a derived type, like GitSourceStepConfig
    public class StepConfigConverter<T> : JsonConverter
        where T : StepConfig
    {
        Dictionary<string, Type> configTypeMap = new Dictionary<string, Type>();

        public void RegisterConfigType<U>(string jsonName)
            where U : T
        {
            configTypeMap.Add(jsonName, typeof(U));
        }

        public override bool CanConvert(Type objectType)
        {
            return (objectType == typeof(T));
        }

        public override object ReadJson(JsonReader reader, Type objectType, object existingValue, JsonSerializer serializer)
        {
            JObject jobj = JObject.Load(reader);
            string jsonType = jobj["Type"].Value<string>();
            Type configType = null;
            bool found = configTypeMap.TryGetValue(jsonType, out configType);

            if (found)
            {
                return jobj.ToObject(configType, serializer);
            }
            else
            {
                throw new Exception($"Could not deserialize pipeline step of type '{jsonType}'");
            }
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
