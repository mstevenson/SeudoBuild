using System;

namespace SeudoBuild.Pipeline.Modules.ShellBuild
{
    public class ShellBuildModule : IBuildModule
    {
        public string Name { get; } = "Shell";

        public Type StepType { get; } = typeof(ShellBuildStep);

        public Type StepConfigType { get; } = typeof(ShellBuildStepConfig);

        public string StepConfigName { get; } = "Shell Build";
    }
}
