using JetBrains.Annotations;

namespace SeudoCI.Pipeline.Modules.SteamDistribute;

[UsedImplicitly]
public class SteamDistributeModule : IDistributeModule
{
    public string Name => "Steam";

    public Type StepType { get; } = typeof(SteamDistributeStep);

    public Type StepConfigType { get; } = typeof(SteamDistributeConfig);

    public string StepConfigName => "Steam Upload";
}