using System;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace UnityBuild
{
    public class BuildStepConfigConverter : JsonConverter
    {
        public override bool CanConvert(Type objectType)
        {
            return (objectType == typeof(BuildStepConfig));
        }
        
        public override object ReadJson(JsonReader reader, Type objectType, object existingValue, JsonSerializer serializer)
        {
            JObject jobj = JObject.Load(reader);
            if (jobj["Type"].Value<string>() == "Shell Script")
                return jobj.ToObject<ShellBuildStepConfig>(serializer);
            
            if (jobj["Type"].Value<string>() == "Unity Execute Method")
                return jobj.ToObject<UnityExecuteMethodBuildConfig>(serializer);
            
            if (jobj["Type"].Value<string>() == "Unity Paramaterized Build")
                return jobj.ToObject<UnityParameterizedBuildConfig>(serializer);
            
            if (jobj["Type"].Value<string>() == "Unity Standard Build")
                return jobj.ToObject<UnityStandardBuildConfig>(serializer);
            
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
