using System;
namespace SeudoBuild
{
    public class GitSourceStep : ISourceStep
    {
        GitSourceConfig config;

        public GitSourceStep(GitSourceConfig config, Workspace workspace)
        {
            this.config = config;
        }

        public string Type { get; } = "Git";

        public SourceStepResults ExecuteStep(Workspace workspace)
        {
            // TODO

            return new SourceStepResults();
        }
    }
}
