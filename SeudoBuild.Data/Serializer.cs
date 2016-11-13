﻿using System.IO;
using Newtonsoft.Json;

namespace SeudoBuild.Data
{
    public class Serializer
    {
        public T Deserialize<T>(string path)
        {
            string json = File.ReadAllText(path);

            JsonConverter[] converters = 
            {
                new SourceStepConfigConverter(),
                new BuildStepConfigConverter(),
                new ArchiveStepConfigConverter(),
                new DistributeStepConfigConverter(),
                new NotifyStepConfigConverter()
            };

            var settings = new JsonSerializerSettings { Converters = converters };

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