namespace SeudoCI.Pipeline.Modules.SFTPDistribute;

public class SFTPDistributeModule : IDistributeModule
{
    public string Name => "SFTP";

    public Type StepType { get; } = typeof(SFTPDistributeStep);

    public Type StepConfigType { get; } = typeof(SFTPDistributeConfig);

    public string StepConfigName => "SFTP Upload";
}