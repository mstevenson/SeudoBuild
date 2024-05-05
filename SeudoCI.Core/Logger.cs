namespace SeudoCI.Core;

using System;

public class Logger : ILogger
{
    private static string _indentString = "";
    private static int _indentLevel;
    public int IndentLevel
    {
        get => _indentLevel;
        set
        {
            if (value == _indentLevel)
            {
                return;
            }
            _indentLevel = value;
            _indentString = _indentLevel > 0 ? new string(' ', _indentLevel * 2) : "";
        }
    }

    public const string Normal = "\x1b[0m";
    public const string BoldOn = "\x1b[1m";
    //public const string BoldOff = "\x1b[21m";
    public const string DimOn = "\x1b[2m";
    //public const string DimOff = "\x1b[22m";
    public const string UnderlineOn = "\x1b[4m";
    //public const string UnderlineOff = "\x1b[24m";
    public const string InvertOn = "\x1b[7m";
    //public const string InvertOff = "\x1b[27m";

    public void Write(string value, LogType logType = LogType.None, LogStyle logStyle = LogStyle.None)
    {
        switch (logStyle) {
            case LogStyle.Bold:
                value = $"{BoldOn}{value}{Normal}";
                break;
            case LogStyle.Dim:
                value = $"{DimOn}{value}{Normal}";
                break;
            case LogStyle.Underline:
                value = $"{UnderlineOn}{value}{Normal}";
                break;
            case LogStyle.Invert:
                value = $"{InvertOn}{value}{Normal}";
                break;
        }

        switch (logType)
        {
            case LogType.None:
            {
                Console.WriteLine($"{_indentString}  {value}");
                break;
            }

            case LogType.Header:
            {
                Console.WriteLine($"{BoldOn}{value}{Normal}");
                break;
            }

            case LogType.Plus:
            {
                Console.ResetColor();
                Console.WriteLine($"{_indentString}‣ {value}");
                break;
            }

            case LogType.Bullet:
            {
                Console.ResetColor();
                Console.WriteLine($"{_indentString}• {value}");
                break;
            }

            case LogType.SmallBullet:
            {
                Console.ResetColor();
                Console.WriteLine($"{_indentString}◦ {value}");
                break;
            }

            case LogType.Success:
            {
                ConsoleColor originalColor = Console.ForegroundColor;
                Console.ForegroundColor = ConsoleColor.Green;
                Console.WriteLine($"{_indentString}✔ {value}");
                Console.ResetColor();
                break;
            }

            case LogType.Failure:
            {
                ConsoleColor originalColor = Console.ForegroundColor;
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine($"{_indentString}✘ {value}");
                Console.ResetColor();
                break;
            }

            case LogType.Alert:
            {
                ConsoleColor originalColor = Console.ForegroundColor;
                Console.ForegroundColor = ConsoleColor.Yellow;
                Console.WriteLine($"{_indentString}! {value}");
                Console.ResetColor();
                break;
            }

            case LogType.Debug:
            {
                ConsoleColor originalColor = Console.ForegroundColor;
                Console.ForegroundColor = ConsoleColor.Magenta;
                Console.WriteLine(value);
                Console.ResetColor();
                break;
            }
            default:
                throw new ArgumentOutOfRangeException(nameof(logType), logType, null);
        }
    }

    public void QueueNotification(string value)
    {
        Console.ForegroundColor = ConsoleColor.Cyan;
        Console.WriteLine($"\n{value}\n");
        Console.ResetColor();
    }
}