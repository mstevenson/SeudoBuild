using System.Collections.Generic;
using System.Text.RegularExpressions;

namespace SeudoBuild.Core
{
    /// <summary>
    /// Register macros to replace in strings.
    /// Macro variables begin and end with the % character.
    /// Example: %project_name% variable could be replaced with the string MyProject.
    /// </summary>
    public class Macros : Dictionary<string, string>, IMacros
    {
        public string ReplaceVariablesInText(string source)
        {
            // Replace all variables that are known by Macros
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
