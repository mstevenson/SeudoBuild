using System;

namespace SeudoCI.Pipeline.Modules.GitSource
{
    public class GitSourceModule : ISourceModule
    {
        public string Name { get; } = "Git";

        public Type StepType { get; } = typeof(GitSourceStep);

        public Type StepConfigType { get; } = typeof(GitSourceConfig);

        public string StepConfigName { get; } = "Git";
    }
}
