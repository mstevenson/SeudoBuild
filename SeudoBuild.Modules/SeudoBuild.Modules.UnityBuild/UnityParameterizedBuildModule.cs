using System;

namespace SeudoBuild.Pipeline.Modules.UnityBuild
{
    public class UnityParameterizedBuildModule : IBuildModule
    {
        public string Name { get; } = "Unity (Parameterized)";

        public Type StepType { get; } = typeof(UnityParameterizedBuildStep);

        public Type StepConfigType { get; } = typeof(UnityParameterizedBuildConfig);

        public string StepConfigName { get; } = "Unity Parameterized Build";
    }
}
