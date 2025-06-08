namespace SeudoCI.Core;

using System.Collections.Generic;
using System.Text.RegularExpressions;

/// <summary>
/// Register macros to replace in strings.
/// Macro variables begin and end with the % character.
/// Example: %project_name% variable could be replaced with the string MyProject.
/// </summary>
public partial class Macros : Dictionary<string, string>, IMacros
{
    public string ReplaceVariablesInText(string source)
    {
        // Replace all variables that are known by Macros
        foreach (var kvp in this)
        {
            source = source.Replace($"%{kvp.Key}%", kvp.Value);
        }
        // Remove any variables that were not matched
        source = MyRegex().Replace(source, string.Empty);
        return source;
    }

    [GeneratedRegex("%.*?%", RegexOptions.Multiline)]
    private static partial Regex MyRegex();
}