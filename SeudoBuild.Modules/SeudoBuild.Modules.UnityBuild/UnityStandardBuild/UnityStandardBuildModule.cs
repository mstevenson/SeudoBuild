using System;
using Newtonsoft.Json;

namespace SeudoBuild.Modules.UnityBuild
{
    public class UnityStandardBuildModule : IBuildModule
    {
        public string Name { get; } = "Unity (Standard)";

        public Type StepType { get; } = typeof(UnityStandardBuildStep);

        public Type StepConfigType { get; } = typeof(UnityStandardBuildConfig);

        public string StepConfigName { get; } = "Unity Standard Build";
    }
}
