using System;
using System.Collections.Generic;
using Newtonsoft.Json;

namespace SeudoBuild.Modules.GitSource
{
    public class GitSourceModule : ISourceModule
    {
        public string Name { get; } = "Git";

        public Type StepType { get; } = typeof(GitSourceStep);

        public Type StepConfigType { get; } = typeof(GitSourceConfig);

        public string StepConfigName { get; } = "Git";
    }
}
