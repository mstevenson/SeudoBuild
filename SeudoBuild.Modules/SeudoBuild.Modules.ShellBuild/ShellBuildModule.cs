using System;
using Newtonsoft.Json;

namespace SeudoBuild.Modules.ShellBuild
{
    public class ShellBuildModule : IBuildModule
    {
        public string Name { get; } = "Shell";

        public Type StepType { get; } = typeof(ShellBuildStep);

        public JsonConverter ConfigConverter { get; } = new ShellBuildConfigConverter();

        public bool MatchesConfigType(StepConfig config)
        {
            return config is ShellBuildStepConfig;
        }
    }
}
