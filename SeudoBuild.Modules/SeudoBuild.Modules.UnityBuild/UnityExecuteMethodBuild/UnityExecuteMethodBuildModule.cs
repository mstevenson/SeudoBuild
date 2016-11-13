using System;
using Newtonsoft.Json;

namespace SeudoBuild.Modules.UnityBuild
{
    public class UnityExecuteMethodBuildModule : IBuildModule
    {
        public string Name { get; } = "Unity (Execute Method)";

        public Type StepType { get; } = typeof(UnityExecuteMethodBuildStep);

        public JsonConverter ConfigConverter { get; } = new UnityBuildConverter();

        public bool MatchesConfigType(StepConfig config)
        {
            return config is UnityExecuteMethodBuildConfig;
        }
    }
}
