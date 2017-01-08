using System;

namespace SeudoBuild
{
    public class BuildConsole : ILogger
    {
        static string indentString = "";
        static int indentLevel;
        public int IndentLevel {
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

        public void WriteLine(string value)
        {
            Console.WriteLine($"{indentString}  {value}");
        }

        public void WriteBullet(string value)
        {
            Console.ResetColor();
            Console.WriteLine($"{indentString}• {value}");
        }

        public void WritePlus(string value)
        {
            Console.ResetColor();
            Console.WriteLine($"{indentString}+ {value}");
        }

        public void WriteSuccess(string value)
        {
            ConsoleColor originalColor = Console.ForegroundColor;
            Console.ForegroundColor = ConsoleColor.Green;
            Console.WriteLine($"{indentString}✔ {value}");
            Console.ResetColor();
        }

        public void WriteFailure(string value)
        {
            ConsoleColor originalColor = Console.ForegroundColor;
            Console.ForegroundColor = ConsoleColor.Red;
            Console.WriteLine($"{indentString}✘ {value}");
            Console.ResetColor();
        }

        public void WriteAlert(string value)
        {
            ConsoleColor originalColor = Console.ForegroundColor;
            Console.ForegroundColor = ConsoleColor.Yellow;
            Console.WriteLine($"{indentString}! {value}");
            Console.ResetColor();
        }

        public void QueueNotification(string value)
        {
            Console.ForegroundColor = ConsoleColor.Cyan;
            Console.WriteLine($"\n{value}\n");
            Console.ResetColor();
        }
    }
}
