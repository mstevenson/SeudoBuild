namespace SeudoBuild
{
    public class SFTPDistributeConfig : DistributeStepConfig
    {
        public override string Type { get; } = "SFTP Upload";
        public string Host { get; set; }
        public string WorkingDirectory { get; set; }
        public int Port { get; set; } = 22;
        public string Username { get; set; }
        public string Password { get; set; }
    }
}
