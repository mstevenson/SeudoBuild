namespace SeudoBuild.Pipeline.Modules.SMBDistribute
{
    /// <summary>
    /// Configuration values for a distribute pipeline step that transfers
    /// a build product via SMB.
    /// </summary>
    public class SMBDistributeConfig : DistributeStepConfig
    {
        public override string Name { get; } = "SMB Transfer";
        public string Host { get; set; }
        public string Directory { get; set; }
        public string Username { get; set; }
        public string Password { get; set; }
    }
}
