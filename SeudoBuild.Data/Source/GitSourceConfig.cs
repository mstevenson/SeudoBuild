namespace SeudoBuild
{
    public class GitSourceConfig : SourceStepConfig
    {
        public override string Type { get; } = "Git";
        public string RepositoryURL { get; set; }
        public string RepositoryBranchName { get; set; }
        public bool UseLFS { get; set; }
        public string Username { get; set; }
        public string Password { get; set; }
    }
}
