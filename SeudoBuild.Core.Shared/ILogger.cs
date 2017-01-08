namespace SeudoBuild
{
    public interface ILogger
    {
        int IndentLevel { get; set; }

        void WriteLine(string value);
        void WriteBullet(string value);
        void WritePlus(string value);
        void WriteSuccess(string value);
        void WriteFailure(string value);
        void WriteAlert(string value);
        void QueueNotification(string value);
    }
}
