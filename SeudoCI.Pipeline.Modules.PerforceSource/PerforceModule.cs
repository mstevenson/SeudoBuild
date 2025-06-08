using JetBrains.Annotations;

namespace SeudoCI.Pipeline.Modules.PerforceSource;

[UsedImplicitly]
public class PerforceModule : ISourceModule
{
    public string Name => "Perforce";
    
    public Type StepType { get; }
    
    public Type StepConfigType { get; }
    public string StepConfigName { get; }
}