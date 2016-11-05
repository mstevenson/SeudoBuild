using System.Collections.Generic;

namespace UnityBuildServer
{
    public class TextReplacements : Dictionary<string, string>
    {
        public string ReplaceVariablesInText(string source)
        {
            foreach (var kvp in this)
            {
                source = source.Replace($"%{kvp.Key}%", kvp.Value);
            }
            return source;
        }
    }
}
