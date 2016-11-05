using System;

namespace UnityBuild
{
    public static class BuildConsole
    {
        static string indentString = "";
        static int indentLevel;
        public static int IndentLevel {
            get {
                return indentLevel;
            }
            set {
                if (value != indentLevel)
                {
                    indentLevel = value;
                    if (indentLevel > 0)
                    {
                        indentString = new string(' ', indentLevel * 2);
                    }
                    else
                    {
                        indentString = "";
                    }
                }
            }
        }

        public static void WriteLine (string value)
        {
            Console.WriteLine($"{indentString}{value}");
        }
    }
}
