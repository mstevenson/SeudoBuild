namespace SeudoCI.Core;

using System;

public class Logger : ILogger
{
    private readonly object _lockObject = new();
    private string _indentString = "";
    private int _indentLevel;

    public int IndentLevel
    {
        get
        {
            lock (_lockObject)
            {
                return _indentLevel;
            }
        }
        set
        {
            lock (_lockObject)
            {
                if (value == _indentLevel)
                {
                    return;
                }
                _indentLevel = value;
                _indentString = _indentLevel > 0 ? new string(' ', _indentLevel * 2) : "";
            }
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

    public void Write(string? value, LogType logType = LogType.None, LogStyle logStyle = LogStyle.None)
    {
        // Apply text styling first
        value = logStyle switch
        {
            LogStyle.Bold => $"{BoldOn}{value}{Normal}",
            LogStyle.Dim => $"{DimOn}{value}{Normal}",
            LogStyle.Underline => $"{UnderlineOn}{value}{Normal}",
            LogStyle.Invert => $"{InvertOn}{value}{Normal}",
            _ => value
        };

        // Get the current indent string in a thread-safe manner
        string currentIndent;
        lock (_lockObject)
        {
            currentIndent = _indentString;
        }

        // Protect all console operations with a lock to prevent interleaved output
        lock (_lockObject)
        {
            switch (logType)
            {
                case LogType.None:
                    Console.WriteLine($"{currentIndent}  {value}");
                    break;

                case LogType.Header:
                    Console.WriteLine($"{BoldOn}{value}{Normal}");
                    break;

                case LogType.Plus:
                    Console.ResetColor();
                    Console.WriteLine($"{currentIndent}‣ {value}");
                    break;

                case LogType.Bullet:
                    Console.ResetColor();
                    Console.WriteLine($"{currentIndent}• {value}");
                    break;

                case LogType.SmallBullet:
                    Console.ResetColor();
                    Console.WriteLine($"{currentIndent}◦ {value}");
                    break;

                case LogType.Success:
                    Console.ForegroundColor = ConsoleColor.Green;
                    Console.WriteLine($"{currentIndent}✔ {value}");
                    Console.ResetColor();
                    break;

                case LogType.Failure:
                    Console.ForegroundColor = ConsoleColor.Red;
                    Console.WriteLine($"{currentIndent}✘ {value}");
                    Console.ResetColor();
                    break;

                case LogType.Alert:
                    Console.ForegroundColor = ConsoleColor.Yellow;
                    Console.WriteLine($"{currentIndent}! {value}");
                    Console.ResetColor();
                    break;

                case LogType.Debug:
                    Console.ForegroundColor = ConsoleColor.Magenta;
                    Console.WriteLine(value);
                    Console.ResetColor();
                    break;

                default:
                    throw new ArgumentOutOfRangeException(nameof(logType), logType, null);
            }
        }
    }

    public void QueueNotification(string value)
    {
        lock (_lockObject)
        {
            Console.ForegroundColor = ConsoleColor.Cyan;
            Console.WriteLine($"\n{value}\n");
            Console.ResetColor();
        }
    }
}