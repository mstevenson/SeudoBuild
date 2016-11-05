using System.Collections.Generic;
using System.Text.RegularExpressions;

namespace UnityBuild
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
            // Replace all variables that are known by TextReplacements
            foreach (var kvp in this)
            {
                source = source.Replace($"%{kvp.Key}%", kvp.Value);
            }
            // Remove any variables that were not matched
            source = Regex.Replace(source, "%.*?%", string.Empty, RegexOptions.Multiline);
            return source;
        }
    }
}
