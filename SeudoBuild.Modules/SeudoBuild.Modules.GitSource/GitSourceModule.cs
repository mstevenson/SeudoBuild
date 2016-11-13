using System;
using Newtonsoft.Json;
using SeudoBuild.Data;

namespace SeudoBuild.Modules.GitSource
{
    public class GitSourceModule : ISourceModule
    {
        public Type StepType { get; } = typeof(GitSourceStep);

        public JsonConverter ConfigConverter { get; } = new GitSourceConfigConverter();

        public bool MatchesConfigType(StepConfig config)
        {
            return config is GitSourceConfig;
        }
    }
}
