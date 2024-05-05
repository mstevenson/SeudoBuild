using System.IO;
using Newtonsoft.Json;

namespace SeudoCI.Core
{
    public class Serializer
    {
        private readonly IFileSystem _fileSystem;

        public string FileExtension { get; } = ".json";

        public Serializer(IFileSystem fileSystem)
        {
            _fileSystem = fileSystem;
        }

        public T Deserialize<T>(string json, JsonConverter[] converters)
        {
            var settings = new JsonSerializerSettings { Converters = converters };
            T obj = JsonConvert.DeserializeObject<T>(json, settings);
            return obj;
        }

        public T DeserializeFromFile<T>(string path, JsonConverter[] converters)
        {
            using (TextReader tr = new StreamReader(_fileSystem.OpenRead(path)))
            {
                var json = tr.ReadToEnd();
                return Deserialize<T>(json, converters);
            }
        }

        public string Serialize<T>(T obj)
        {
            var json = JsonConvert.SerializeObject(obj, Formatting.Indented);
            return json;
        }

        public void SerializeToFile<T>(T obj, string path)
        {
            if (_fileSystem.FileExists(path))
            {
                _fileSystem.DeleteFile(path);
            }
            var json = Serialize(obj);
            _fileSystem.WriteAllText(path, json);
        }
    }
}
