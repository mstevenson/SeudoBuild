namespace SeudoCI.Core
{
    public interface IMacros
    {
        string ReplaceVariablesInText(string source);

        string this[string index] { get; set; }
    }
}
