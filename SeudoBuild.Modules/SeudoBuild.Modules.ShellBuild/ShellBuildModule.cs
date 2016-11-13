using System;
using Newtonsoft.Json;
using SeudoBuild.Data;

namespace SeudoBuild.Modules.ShellBuild
{
    public class ShellBuildModule : IBuildModule
    {
        public Type StepType { get; } = typeof(ShellBuildStep);

        public JsonConverter ConfigConverter { get; } = new ShellBuildConfigConverter();

        public bool MatchesConfigType(StepConfig config)
        {
            return config is ShellBuildStepConfig;
        }
    }
}
