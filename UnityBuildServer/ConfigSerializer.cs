using System.IO;
using Newtonsoft.Json;

namespace UnityBuildServer
{
    public class ConfigSerializer
    {
        public T Deserialize<T>(string path)
        {
            string json = File.ReadAllText(path);
            T obj = JsonConvert.DeserializeObject<T>(json);
            return obj;
        }

        public void Serialize<T>(T obj, string path)
        {
            string json = JsonConvert.SerializeObject(obj, Formatting.Indented);
            File.WriteAllText(path, json);
        }
    }
}
