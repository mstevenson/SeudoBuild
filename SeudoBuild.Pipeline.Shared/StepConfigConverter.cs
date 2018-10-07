using System;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System.Collections.Generic;

namespace SeudoBuild.Pipeline
{
    /// <inheritdoc />
    /// <summary>
    /// Converts a StepConfig object to and from a serialized JSON representation.
    /// </summary>
    public class StepConfigConverter : JsonConverter
    {
        private readonly Type _configBaseType;
        private readonly Dictionary<string, Type> _configTypeMap = new Dictionary<string, Type>();

        public StepConfigConverter(Type configBaseType)
        {
            if (configBaseType == typeof(StepConfig) || !typeof(StepConfig).IsAssignableFrom(configBaseType))
            {
                throw new ArgumentException("StepConfigConverter must be given a type that inherits from StepConfig");
            }
            _configBaseType = configBaseType;
        }

        public void RegisterConfigType(string jsonName, Type type)
        {
            if (!_configBaseType.IsAssignableFrom(type))
            {
                throw new ArgumentException($"Type {type} is not assignable from {_configBaseType}");
            }
            _configTypeMap.Add(jsonName, type);
        }

        public override bool CanConvert(Type objectType)
        {
            return (objectType == _configBaseType);
        }

        public override object ReadJson(JsonReader reader, Type objectType, object existingValue, JsonSerializer serializer)
        {
            JObject jobj = JObject.Load(reader);
            string jsonType = jobj["Type"].Value<string>();
            bool found = _configTypeMap.TryGetValue(jsonType, out var configType);

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

    /// <inheritdoc />
    /// <summary>
    /// Converts a StepConfig object of the given subtype to and from
    /// a serialized JSON representation.
    /// </summary>
    public class StepConfigConverter<T> : StepConfigConverter
        where T : StepConfig
    {
        public StepConfigConverter() : base(typeof(T)) {}
    }
}
