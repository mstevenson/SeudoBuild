namespace SeudoBuild
{
    public interface ILogger
    {
        int IndentLevel { get; set; }

        void Write(string value, LogType logType = LogType.None);
        void QueueNotification(string value);
    }
}
