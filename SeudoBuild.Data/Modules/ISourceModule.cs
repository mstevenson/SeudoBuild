using System;
using Newtonsoft.Json;

namespace SeudoBuild.Data
{
    public interface ISourceModule
    {
        Type ArchiveStepType { get; }
        JsonConverter ConfigConverter { get; }
        bool MatchesConfigType(SourceStepConfig config);
    }
}
