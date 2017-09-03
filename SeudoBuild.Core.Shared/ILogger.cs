namespace SeudoBuild
{
    public interface ILogger
    {
        int IndentLevel { get; set; }

        void Write(string value, LogType logType = LogType.None, LogStyle logStyle = LogStyle.None);
        void QueueNotification(string value);
    }
}
