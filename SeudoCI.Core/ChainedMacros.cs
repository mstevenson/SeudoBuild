namespace SeudoCI.Core;

using System.Collections.Generic;
using System.Text.RegularExpressions;

/// <summary>
/// A macros implementation that chains lookups - first checking its own values,
/// then falling back to a parent macros instance if provided.
/// </summary>
public partial class ChainedMacros(IMacros? parentMacros = null) : IMacros
{
    private readonly Dictionary<string, string> _localMacros = new();

    public string this[string index]
    {
        get
        {
            if (_localMacros.TryGetValue(index, out var value))
                return value;
            
            if (parentMacros != null)
                return parentMacros[index];
            
            throw new KeyNotFoundException($"Macro '{index}' not found.");
        }
        set
        {
            _localMacros[index] = value;
        }
    }

    public string ReplaceVariablesInText(string source)
    {
        // Get all macros from both local and parent
        var allMacros = GetAllMacros();
        
        // Replace all variables that are known by Macros
        foreach (var kvp in allMacros)
        {
            source = source.Replace($"%{kvp.Key}%", kvp.Value);
        }
        
        // Remove any variables that were not matched
        source = MyRegex().Replace(source, string.Empty);
        return source;
    }

    private Dictionary<string, string> GetAllMacros()
    {
        var result = new Dictionary<string, string>();
        
        // Add parent macros first (if any)
        if (parentMacros != null)
        {
            // Handle different parent macro implementations
            if (parentMacros is Macros parentDict)
            {
                foreach (var kvp in parentDict)
                {
                    result[kvp.Key] = kvp.Value;
                }
            }
            else if (parentMacros is ChainedMacros parentChained)
            {
                // Recursively get all macros from chained parent
                var parentMacros = parentChained.GetAllMacros();
                foreach (var kvp in parentMacros)
                {
                    result[kvp.Key] = kvp.Value;
                }
            }
            // For other IMacros implementations, we can't enumerate easily
            // They would need to be accessed via the indexer
        }
        
        // Add local macros (overriding parent if duplicate keys)
        foreach (var kvp in _localMacros)
        {
            result[kvp.Key] = kvp.Value;
        }
        
        return result;
    }

    [GeneratedRegex("%.*?%", RegexOptions.Multiline)]
    private static partial Regex MyRegex();
}