using System.Collections.Generic;

namespace SeudoBuild
{
    public interface IMacros
    {
        string ReplaceVariablesInText(string source);

        string this[string index] { get; set; }
    }
}
