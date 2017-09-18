using System.Collections.Generic;
using System.IO;
using Yaml = YamlDotNet.Serialization;

namespace SeudoBuild
{
    public class Serializer
    {
        // How to map tags to types with YAML:
        // https://stackoverflow.com/questions/38248697/how-to-serialize-deserialize-list-with-interface

        readonly IFileSystem fileSystem;

        readonly Yaml.INamingConvention namingConvention = new Yaml.NamingConventions.HyphenatedNamingConvention();

        IEnumerable<SerializedTypeMap> serializedTypeMaps;

        Yaml.Serializer GetSerializer()
        {
            var builder = new Yaml.SerializerBuilder();
            builder.EmitDefaults();
            builder.EnsureRoundtrip();
            builder.WithNamingConvention(namingConvention);
            if (serializedTypeMaps != null)
            {
                foreach (var type in serializedTypeMaps)
                {
                    builder.WithTagMapping(GetFullTag(type.tag), type.type);
                }
            }
            var serializer = builder.Build();
            return serializer;
        }

        Yaml.Deserializer GetDeserializer()
        {
            var builder = new Yaml.DeserializerBuilder();
            builder.WithNamingConvention(namingConvention);
            if (serializedTypeMaps != null)
            {
                foreach (var type in serializedTypeMaps)
                {
                    builder.WithTagMapping(GetFullTag(type.tag), type.type);
                }
            }
            var deserializer = builder.Build();
            return deserializer;
        }

        string GetFullTag(string shortTag)
        {
            return $"tag:yaml.org,2002:{shortTag}";
        }

        public Serializer(IFileSystem fileSystem, IEnumerable<SerializedTypeMap> serializedTypeMaps)
        {
            this.fileSystem = fileSystem;
            this.serializedTypeMaps = serializedTypeMaps;
        }

        public T Deserialize<T>(string yaml)
        {
            var result = GetDeserializer().Deserialize<T>(yaml);
            return result;
        }

        public T DeserializeFromFile<T>(string path)
        {
            using (var reader = File.OpenText(path))
            {
                var result = GetDeserializer().Deserialize<T>(reader);
                return result;
            }
        }

        public string Serialize<T>(T obj)
        {
            string result = GetSerializer().Serialize(obj);
            return result;
        }

        public void SerializeToFile<T>(T obj, string path)
        {
            using (var writer = File.CreateText(path))
            {
                GetSerializer().Serialize(writer, obj);
            }
        }
    }
}
