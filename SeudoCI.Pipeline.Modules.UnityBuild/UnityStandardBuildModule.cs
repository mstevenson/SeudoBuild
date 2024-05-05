namespace SeudoCI.Pipeline.Modules.UnityBuild;

public class UnityStandardBuildModule : IBuildModule
{
    public string Name => "Unity (Standard)";

    public Type StepType { get; } = typeof(UnityStandardBuildStep);

    public Type StepConfigType { get; } = typeof(UnityStandardBuildConfig);

    public string StepConfigName => "Unity Standard Build";
}