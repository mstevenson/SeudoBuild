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

        public T Deserialize<T>(string path, JsonConverter[] converters)
        {
            using (TextReader tr = new StreamReader(fileSystem.OpenRead(path)))
            {
                string json = tr.ReadToEnd();
                var settings = new JsonSerializerSettings { Converters = converters };
                T obj = JsonConvert.DeserializeObject<T>(json, settings);
                return obj;
            }
        }

        public void Serialize<T>(T obj, string path)
        {
            if (fileSystem.FileExists(path))
            {
                fileSystem.DeleteFile(path);
            }
            using (TextWriter tw = new StreamWriter(fileSystem.OpenWrite(path)))
            {
                string json = JsonConvert.SerializeObject(obj, Formatting.Indented);
                tw.Write(path, json);
            }
        }
    }
}
