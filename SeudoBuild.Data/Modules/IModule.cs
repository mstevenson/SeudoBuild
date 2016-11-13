using System;
using Newtonsoft.Json;

namespace SeudoBuild.Data
{
    public interface IModule
    {
        Type StepType { get; }
        JsonConverter ConfigConverter { get; }
        bool MatchesConfigType(StepConfig config);
    }
}
