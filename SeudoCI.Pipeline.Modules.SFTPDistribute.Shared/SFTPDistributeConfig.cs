namespace SeudoCI.Pipeline.Modules.SFTPDistribute
{
    /// <summary>
    /// Configuration values for a distribute pipeline step that uploads
    /// a build product via SFTP.
    /// </summary>
    public class SFTPDistributeConfig : DistributeStepConfig
    {
        public override string Name { get; } = "SFTP Upload";
        public string Host { get; set; }
        public string WorkingDirectory { get; set; }
        public int Port { get; set; } = 22;
        public string Username { get; set; }
        public string Password { get; set; }
    }
}
