using System;
using Newtonsoft.Json;

namespace SeudoBuild
{
    public interface IDistributeModule
    {
        Type ArchiveStepType { get; }
        JsonConverter ConfigConverter { get; }
        bool MatchesConfigType(DistributeStepConfig config);
    }
}
