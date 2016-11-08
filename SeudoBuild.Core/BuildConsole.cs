using System;

namespace SeudoBuild
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

        public static void WriteLine(string value)
        {
            Console.WriteLine($"{indentString}  {value}");
        }

        public static void WriteBullet(string value)
        {
            Console.ResetColor();
            Console.WriteLine($"{indentString}• {value}");
        }

        public static void WriteSuccess(string value)
        {
            ConsoleColor originalColor = Console.ForegroundColor;
            Console.ForegroundColor = ConsoleColor.Green;
            Console.WriteLine($"{indentString}✔ {value}");
            Console.ResetColor();
        }

        public static void WriteFailure(string value)
        {
            ConsoleColor originalColor = Console.ForegroundColor;
            Console.ForegroundColor = ConsoleColor.Red;
            Console.WriteLine($"{indentString}✘ {value}");
            Console.ResetColor();
        }

        public static void WriteAlert(string value)
        {
            ConsoleColor originalColor = Console.ForegroundColor;
            Console.ForegroundColor = ConsoleColor.Yellow;
            Console.WriteLine($"{indentString}! {value}");
            Console.ResetColor();
        }
    }
}
