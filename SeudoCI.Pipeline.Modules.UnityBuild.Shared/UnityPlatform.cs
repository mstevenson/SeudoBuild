namespace SeudoCI.Pipeline.Modules.UnityBuild;

using Newtonsoft.Json;
using Newtonsoft.Json.Converters;

[JsonConverter(typeof(StringEnumConverter))]
public enum UnityPlatform
{
    Mac,
    Windows,
    Linux
}