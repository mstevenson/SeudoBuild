using System;
using Newtonsoft.Json;

namespace SeudoBuild.Modules.UnityBuild
{
    public class UnityParameterizedBuildModule : IBuildModule
    {
        public Type StepType { get; } = typeof(UnityParameterizedBuildStep);

        public JsonConverter ConfigConverter { get; } = new UnityBuildConverter();

        public bool MatchesConfigType(StepConfig config)
        {
            return config is UnityParameterizedBuildConfig;
        }
    }
}
