namespace SeudoBuild.Pipeline.Modules.SMBDistribute
{
    public class SMBDistributeConfig : DistributeStepConfig
    {
        public override string Type { get; } = "SMB Transfer";
        public string Host { get; set; }
        public string Directory { get; set; }
        public string Username { get; set; }
        public string Password { get; set; }
    }
}
