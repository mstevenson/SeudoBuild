using System;
using System.Collections.Generic;

namespace SeudoBuild.Modules.UnityBuild
{
    public class UnityExecuteMethodBuildModule : IBuildModule
    {
        public string Name { get; } = "Unity (Execute Method)";

        public Type StepType { get; } = typeof(UnityExecuteMethodBuildStep);

        public Type StepConfigType { get; } = typeof(UnityExecuteMethodBuildConfig);

        public string StepConfigName { get; } = "Unity Execute Method";
    }
}
