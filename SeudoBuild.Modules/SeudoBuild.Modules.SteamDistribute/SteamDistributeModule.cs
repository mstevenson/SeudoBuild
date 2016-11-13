using System;
using Newtonsoft.Json;

namespace SeudoBuild.Modules.SteamDistribute
{
    public class SteamDistributeModule : IDistributeModule
    {
        public Type StepType { get; } = typeof(SteamDistributeStep);

        public JsonConverter ConfigConverter { get; } = new SteamDistributeConfigConverter();

        public bool MatchesConfigType(StepConfig config)
        {
            return config is SteamDistributeConfig;
        }
    }
}
