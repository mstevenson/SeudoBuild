using System;

namespace SeudoBuild
{
    public class Logger : ILogger
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

        public void Write(string value, LogType logType = LogType.None)
        {
            switch (logType)
            {
                case LogType.None:
                {
                    Console.WriteLine($"{indentString}  {value}");
                    break;
                }

                case LogType.Plus:
                {
                    Console.ResetColor();
                    Console.WriteLine($"{indentString}+ {value}");
                    break;
                }

                case LogType.Bullet:
                {
                    Console.ResetColor();
                    Console.WriteLine($"{indentString}• {value}");
                    break;
                }

                case LogType.Success:
                {
                    ConsoleColor originalColor = Console.ForegroundColor;
                    Console.ForegroundColor = ConsoleColor.Green;
                    Console.WriteLine($"{indentString}✔ {value}");
                    Console.ResetColor();
                    break;
                }

                case LogType.Failure:
                {
                    ConsoleColor originalColor = Console.ForegroundColor;
                    Console.ForegroundColor = ConsoleColor.Red;
                    Console.WriteLine($"{indentString}✘ {value}");
                    Console.ResetColor();
                    break;
                }

                case LogType.Alert:
                {
                    ConsoleColor originalColor = Console.ForegroundColor;
                    Console.ForegroundColor = ConsoleColor.Red;
                    Console.WriteLine($"{indentString}✘ {value}");
                    Console.ResetColor();
                    break;
                }
            }
        }

        public void QueueNotification(string value)
        {
            Console.ForegroundColor = ConsoleColor.Cyan;
            Console.WriteLine($"\n{value}\n");
            Console.ResetColor();
        }
    }
}
