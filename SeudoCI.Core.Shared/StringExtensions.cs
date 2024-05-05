namespace SeudoCI.Core;

using System.Text.RegularExpressions;

public static class StringExtensions
{
    private static readonly Regex Regex;

    static StringExtensions()
    {
        var regexString = new string(Path.GetInvalidFileNameChars());
        Regex = new Regex($"[{Regex.Escape(regexString)}]");
    }

    public static string SanitizeFilename(this string filename)
    {
        return Regex.Replace(filename, "").Replace(' ', '_');
    }
}