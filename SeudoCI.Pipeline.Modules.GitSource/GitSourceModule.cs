using JetBrains.Annotations;

namespace SeudoCI.Pipeline.Modules.GitSource;

[UsedImplicitly]
public class GitSourceModule : ISourceModule
{
    public string Name => "Git";

    public Type StepType { get; } = typeof(GitSourceStep);

    public Type StepConfigType { get; } = typeof(GitSourceConfig);

    public string StepConfigName => "Git";
}