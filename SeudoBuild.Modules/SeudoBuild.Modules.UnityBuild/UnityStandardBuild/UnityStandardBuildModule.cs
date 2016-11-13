using System;
using Newtonsoft.Json;

namespace SeudoBuild.Modules.UnityBuild
{
    public class UnityStandardBuildModule : IBuildModule
    {
        public Type ArchiveStepType { get; } = typeof(UnityStandardBuildStep);

        public JsonConverter ConfigConverter { get; } = new UnityBuildConverter();

        public bool MatchesConfigType(BuildStepConfig config)
        {
            return config is UnityStandardBuildConfig;
        }
    }
}
