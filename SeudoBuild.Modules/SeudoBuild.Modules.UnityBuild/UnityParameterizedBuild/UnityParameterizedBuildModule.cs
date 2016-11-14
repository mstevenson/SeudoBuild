using System;
using Newtonsoft.Json;

namespace SeudoBuild.Modules.UnityBuild
{
    public class UnityParameterizedBuildModule : IBuildModule
    {
        public string Name { get; } = "Unity (Parameterized)";

        public Type StepType { get; } = typeof(UnityParameterizedBuildStep);

        public JsonConverter ConfigConverter { get; } = new UnityBuildConverter();

        public bool CanReadConfig(StepConfig config)
        {
            return config is UnityParameterizedBuildConfig;
        }
    }
}
