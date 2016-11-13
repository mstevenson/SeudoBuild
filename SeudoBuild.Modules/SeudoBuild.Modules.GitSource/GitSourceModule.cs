using System;
using Newtonsoft.Json;

namespace SeudoBuild.Modules.GitSource
{
    public class GitSourceModule : ISourceModule
    {
        public Type ArchiveStepType { get; } = typeof(GitSourceStep);

        public JsonConverter ConfigConverter { get; } = new GitSourceConfigConverter();

        public bool MatchesConfigType(SourceStepConfig config)
        {
            return config is GitSourceConfig;
        }
    }
}
