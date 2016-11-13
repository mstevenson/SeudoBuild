using System;
using Newtonsoft.Json;

namespace SeudoBuild.Modules.ShellBuild
{
    public class ShellBuildModule : IBuildModule
    {
        public Type ArchiveStepType { get; } = typeof(ShellBuildStep);

        public JsonConverter ConfigConverter { get; } = new ShellBuildConfigConverter();

        public bool MatchesConfigType(BuildStepConfig config)
        {
            return config is ShellBuildStepConfig;
        }
    }
}
