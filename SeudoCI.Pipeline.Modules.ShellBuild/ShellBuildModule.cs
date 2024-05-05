namespace SeudoCI.Pipeline.Modules.ShellBuild;

public class ShellBuildModule : IBuildModule
{
    public string Name => "Shell";

    public Type StepType { get; } = typeof(ShellBuildStep);

    public Type StepConfigType { get; } = typeof(ShellBuildStepConfig);

    public string StepConfigName => "Shell Build";
}