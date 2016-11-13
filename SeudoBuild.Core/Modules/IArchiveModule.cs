using System;
using Newtonsoft.Json;

namespace SeudoBuild
{
    public interface IArchiveModule
    {
        Type ArchiveStepType { get; }
        JsonConverter ConfigConverter { get; }
        bool MatchesConfigType(ArchiveStepConfig config);
    }
}
