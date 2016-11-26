using Newtonsoft.Json;
using Newtonsoft.Json.Converters;

namespace SeudoBuild.Pipeline.Modules.UnityBuild
{
    [JsonConverter(typeof(StringEnumConverter))]
    public enum UnityPlatform
    {
        Mac,
        Windows,
        Linux
    }
}
