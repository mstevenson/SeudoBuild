using Newtonsoft.Json;
using Newtonsoft.Json.Converters;

namespace SeudoBuild
{
    [JsonConverter(typeof(StringEnumConverter))]
    public enum UnityPlatform
    {
        Mac,
        Windows,
        Linux
    }
}
