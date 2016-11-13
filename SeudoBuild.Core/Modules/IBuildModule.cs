using System;
using Newtonsoft.Json;

namespace SeudoBuild
{
    public interface IBuildModule
    {
        Type ArchiveStepType { get; }
        JsonConverter ConfigConverter { get; }
        bool MatchesConfigType(BuildStepConfig config);
    }
}
