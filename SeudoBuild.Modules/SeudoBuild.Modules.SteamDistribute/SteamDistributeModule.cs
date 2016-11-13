using System;
using Newtonsoft.Json;
using SeudoBuild.Data;

namespace SeudoBuild.Modules.SteamDistribute
{
    public class SteamDistributeModule : IDistributeModule
    {
        public Type ArchiveStepType { get; } = typeof(SteamDistributeStep);

        public JsonConverter ConfigConverter { get; } = new SteamDistributeConfigConverter();

        public bool MatchesConfigType(DistributeStepConfig config)
        {
            return config is SteamDistributeConfig;
        }
    }
}
