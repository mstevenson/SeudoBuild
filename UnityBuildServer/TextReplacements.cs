using System.Collections.Generic;

namespace UnityBuildServer
{
    /// <summary>
    /// Register in-line variables to replace in strings and their replacement values.
    /// Variables begin and end with the % character.
    /// Example: %project_name% variable could be replaced with the string MyProject.
    /// </summary>
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
