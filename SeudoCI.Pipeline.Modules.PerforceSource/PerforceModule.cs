using JetBrains.Annotations;

namespace SeudoCI.Pipeline.Modules.PerforceSource;

using Shared;

[UsedImplicitly]
public class PerforceModule : ISourceModule
{
    public string Name => "Perforce";

    public Type StepType { get; } = typeof(PerforceStep);

    public Type StepConfigType { get; } = typeof(PerforceConfig);

    public string StepConfigName => "Perforce";
}
