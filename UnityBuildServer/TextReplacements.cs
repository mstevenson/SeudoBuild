using System.Collections.Generic;

namespace UnityBuildServer
{
    // FIXME A TextReplacements instance should be owned by the active Workspace

    public static class TextReplacements
    {
        static Dictionary<string, string> replacements = new Dictionary<string, string>();

        public static void RegisterReplacement (string placeholder, string replacement)
        {
            replacements.Add(placeholder, replacement);
        }

        public static string FillPlaceholders(string text)
        {
            foreach (var kvp in replacements)
            {
                text = text.Replace($"%{kvp.Key}%", kvp.Value);
            }
            return text;
        }
    }
}
