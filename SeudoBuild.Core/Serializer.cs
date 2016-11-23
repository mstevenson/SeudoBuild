using Newtonsoft.Json;
using System.IO;

namespace SeudoBuild
{
    public class Serializer
    {
        IFileSystem fileSystem;

        public Serializer(IFileSystem fileSystem)
        {
            this.fileSystem = fileSystem;
        }

        public T Deserialize<T>(string json, JsonConverter[] converters)
        {
            var settings = new JsonSerializerSettings { Converters = converters };
            T obj = JsonConvert.DeserializeObject<T>(json, settings);
            return obj;
        }

        public T DeserializeFromFile<T>(string path, JsonConverter[] converters)
        {
            using (TextReader tr = new StreamReader(fileSystem.OpenRead(path)))
            {
                string json = tr.ReadToEnd();
                return Deserialize<T>(json, converters);
            }
        }

        public string Serialize<T>(T obj)
        {
            string json = JsonConvert.SerializeObject(obj, Formatting.Indented);
            return json;
        }

        public void SerializeToFile<T>(T obj, string path)
        {
            if (fileSystem.FileExists(path))
            {
                fileSystem.DeleteFile(path);
            }
            using (TextWriter tw = new StreamWriter(fileSystem.OpenWrite(path)))
            {
                string json = Serialize(obj);
                tw.Write(path, json);
            }
        }
    }
}
