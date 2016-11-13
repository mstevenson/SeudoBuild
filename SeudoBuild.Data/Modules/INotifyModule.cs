using System;
using Newtonsoft.Json;

namespace SeudoBuild.Data
{
    public interface INotifyModule
    {
        Type ArchiveStepType { get; }
        JsonConverter ConfigConverter { get; }
        bool MatchesConfigType(NotifyStepConfig config);
    }
}
