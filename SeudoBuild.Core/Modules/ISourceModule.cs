using System;
using Newtonsoft.Json;

namespace SeudoBuild
{
    public interface ISourceModule
    {
        Type ArchiveStepType { get; }
        JsonConverter ConfigConverter { get; }
        bool MatchesConfigType(SourceStepConfig config);
    }
}
