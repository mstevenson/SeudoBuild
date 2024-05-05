namespace SeudoCI.Core;

using Newtonsoft.Json;

public class Serializer(IFileSystem fileSystem)
{
    public string FileExtension { get; } = ".json";

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
        if (fileSystem.FileExists(path))
        {
            fileSystem.DeleteFile(path);
        }
        var json = Serialize(obj);
        fileSystem.WriteAllText(path, json);
    }
}