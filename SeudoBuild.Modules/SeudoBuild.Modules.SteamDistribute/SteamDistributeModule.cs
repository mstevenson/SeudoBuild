using System;

namespace SeudoBuild.Pipeline.Modules.SteamDistribute
{
    public class SteamDistributeModule : IDistributeModule
    {
        public string Name { get; } = "Steam";

        public Type StepType { get; } = typeof(SteamDistributeStep);

        public Type StepConfigType { get; } = typeof(SteamDistributeConfig);

        public string StepConfigName { get; } = "Steam Upload";
    }
}
