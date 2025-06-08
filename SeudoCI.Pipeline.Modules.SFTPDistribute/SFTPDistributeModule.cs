using JetBrains.Annotations;

namespace SeudoCI.Pipeline.Modules.SFTPDistribute;

[UsedImplicitly]
public class SFTPDistributeModule : IDistributeModule
{
    public string Name => "SFTP";

    public Type StepType { get; } = typeof(SFTPDistributeStep);

    public Type StepConfigType { get; } = typeof(SFTPDistributeConfig);

    public string StepConfigName => "SFTP Upload";
}