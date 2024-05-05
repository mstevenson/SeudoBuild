namespace SeudoBuild.Pipeline.Modules.Perforce;

public class PerforceModule : ISourceModule
{
    public string Name => "Perforce";
    
    public Type StepType { get; }
    
    public Type StepConfigType { get; }
    public string StepConfigName { get; }
}