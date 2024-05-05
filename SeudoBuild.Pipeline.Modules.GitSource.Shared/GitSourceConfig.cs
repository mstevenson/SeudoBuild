namespace SeudoBuild.Pipeline.Modules.GitSource
{
    /// <summary>
    /// Configuration values for a source pipeline step that clones a git repository.
    /// </summary>
    public class GitSourceConfig : SourceStepConfig
    {
        public override string Name { get; } = "Git";
        public string RepositoryURL { get; set; }
        public string RepositoryBranchName { get; set; }
        public bool UseLFS { get; set; }
        public string Username { get; set; }
        public string Password { get; set; }
    }
}
