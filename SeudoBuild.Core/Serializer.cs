using System.IO;
using Newtonsoft.Json;

namespace SeudoBuild
{
    public class Serializer
    {
        public T Deserialize<T>(string path)
        {
            string json = File.ReadAllText(path);


            // TODO collect json converters from all loaded module DLLs


            //JsonConverter[] converters = 
            //{
            //    new SourceStepConfigConverter(),
            //    new BuildStepConfigConverter(),
            //    new ArchiveStepConfigConverter(),
            //    new DistributeStepConfigConverter(),
            //    new NotifyStepConfigConverter()
            //};


            //var settings = new JsonSerializerSettings { Converters = converters };
            var settings = new JsonSerializerSettings();


            T obj = JsonConvert.DeserializeObject<T>(json, settings);
            return obj;
        }

        public void Serialize<T>(T obj, string path)
        {
            string json = JsonConvert.SerializeObject(obj, Formatting.Indented);
            File.WriteAllText(path, json);
        }
    }
}
