namespace SeudoCI.Core;

using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using SeudoCI.Core.Serialization;
using YamlDotNet.Serialization;
using YamlDotNet.Serialization.NamingConventions;

public class Serializer(IFileSystem fileSystem)
{
    public string FileExtension { get; } = ".yaml";

    private IDeserializer BuildDeserializer(ITypeDiscriminator[] discriminators)
    {
        var builder = new DeserializerBuilder()
            .WithNamingConvention(CamelCaseNamingConvention.Instance)
            .IgnoreUnmatchedProperties();

        if (discriminators.Length > 0)
        {
            builder = builder.WithTypeDiscriminatingNodeDeserializer(options =>
            {
                var method = options.GetType()
                    .GetMethods(BindingFlags.Public | BindingFlags.Instance)
                    .First(m => m.IsGenericMethodDefinition && m.Name == "AddKeyValueTypeDiscriminator" && m.GetParameters().Length == 2);

                foreach (var discriminator in discriminators)
                {
                    var mapping = new Dictionary<string, Type>(StringComparer.OrdinalIgnoreCase);
                    foreach (var kvp in discriminator.Mappings)
                    {
                        mapping[kvp.Key] = kvp.Value;
                    }

                    var genericMethod = method.MakeGenericMethod(discriminator.BaseType);
                    genericMethod.Invoke(options, new object[] { "type", mapping });
                    genericMethod.Invoke(options, new object[] { "Type", mapping });
                }
            });
        }

        return builder.Build();
    }

    public T Deserialize<T>(string yaml, ITypeDiscriminator[] discriminators)
    {
        var deserializer = BuildDeserializer(discriminators);
        var obj = deserializer.Deserialize<T>(yaml);
        return obj ?? throw new InvalidOperationException("Failed to deserialize YAML");
    }

    public T DeserializeFromFile<T>(string path, ITypeDiscriminator[] discriminators)
    {
        using TextReader tr = new StreamReader(fileSystem.OpenRead(path));
        var yaml = tr.ReadToEnd();
        return Deserialize<T>(yaml, discriminators);
    }

    public string Serialize<T>(T obj)
    {
        var serializer = new SerializerBuilder()
            .WithNamingConvention(CamelCaseNamingConvention.Instance)
            .ConfigureDefaultValuesHandling(DefaultValuesHandling.OmitNull)
            .Build();
        var yaml = serializer.Serialize(obj);
        return yaml;
    }

    public void SerializeToFile<T>(T obj, string path)
    {
        if (fileSystem.FileExists(path))
        {
            fileSystem.DeleteFile(path);
        }
        var yaml = Serialize(obj);
        fileSystem.WriteAllText(path, yaml);
    }
}