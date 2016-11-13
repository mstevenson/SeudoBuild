using System;
using Newtonsoft.Json;

namespace SeudoBuild.Modules.UnityBuild
{
    public class UnityExecuteMethodBuildModule : IBuildModule
    {
        public Type ArchiveStepType { get; } = typeof(UnityExecuteMethodBuildStep);

        public JsonConverter ConfigConverter { get; } = new UnityBuildConverter();

        public bool MatchesConfigType(BuildStepConfig config)
        {
            return config is UnityExecuteMethodBuildConfig;
        }
    }
}
