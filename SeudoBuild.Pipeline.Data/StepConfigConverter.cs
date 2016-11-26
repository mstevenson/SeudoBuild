using System;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System.Collections.Generic;

namespace SeudoBuild.Pipeline
{
    public class StepConfigConverter : JsonConverter
    {
        readonly Type configBaseType;
        readonly Dictionary<string, Type> configTypeMap = new Dictionary<string, Type>();

        public StepConfigConverter(Type configBaseType)
        {
            if (configBaseType == typeof(StepConfig) || !typeof(StepConfig).IsAssignableFrom(configBaseType))
            {
                throw new ArgumentException("StepConfigConverter must be given a type that inherits from StepConfig");
            }
            this.configBaseType = configBaseType;
        }

        public void RegisterConfigType(string jsonName, Type type)
        {
            if (!configBaseType.IsAssignableFrom(type))
            {
                throw new ArgumentException($"Type {type} is not assignable from {configBaseType}");
            }
            configTypeMap.Add(jsonName, type);
        }

        public override bool CanConvert(Type objectType)
        {
            return (objectType == configBaseType);
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

    public class StepConfigConverter<T> : StepConfigConverter
        where T : StepConfig
    {
        public StepConfigConverter() : base(typeof(T)) {}
    }
}
