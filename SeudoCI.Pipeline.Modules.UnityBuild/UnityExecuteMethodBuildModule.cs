﻿using JetBrains.Annotations;

namespace SeudoCI.Pipeline.Modules.UnityBuild;

[UsedImplicitly]
public class UnityExecuteMethodBuildModule : IBuildModule
{
    public string Name => "Unity (Execute Method)";

    public Type StepType { get; } = typeof(UnityExecuteMethodBuildStep);

    public Type StepConfigType { get; } = typeof(UnityExecuteMethodBuildConfig);

    public string StepConfigName => "Unity Execute Method";
}