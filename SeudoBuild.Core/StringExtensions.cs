using System.IO;
using System.Text.RegularExpressions;

namespace SeudoBuild
{
    public static class StringExtensions
    {
        static Regex regex;

        static StringExtensions()
        {
            string regexString = new string(Path.GetInvalidFileNameChars());
            regex = new Regex(string.Format("[{0}]", Regex.Escape(regexString)));
        }

        public static string SanitizeFilename(this string filename)
        {
            return regex.Replace(filename, "").Replace(' ', '_');
        }
    }
}
