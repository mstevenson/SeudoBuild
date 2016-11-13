using System;
using Newtonsoft.Json;
using SeudoBuild.Data;

namespace SeudoBuild.Modules.UnityBuild
{
    public class UnityExecuteMethodBuildModule : IBuildModule
    {
        public Type StepType { get; } = typeof(UnityExecuteMethodBuildStep);

        public JsonConverter ConfigConverter { get; } = new UnityBuildConverter();

        public bool MatchesConfigType(StepConfig config)
        {
            return config is UnityExecuteMethodBuildConfig;
        }
    }
}
