namespace SeudoCI.Pipeline.Modules.UnityBuild;

public class UnityParameterizedBuildModule : IBuildModule
{
    public string Name => "Unity (Parameterized)";

    public Type StepType { get; } = typeof(UnityParameterizedBuildStep);

    public Type StepConfigType { get; } = typeof(UnityParameterizedBuildConfig);

    public string StepConfigName => "Unity Parameterized Build";
}