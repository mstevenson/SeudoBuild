namespace SeudoCI.Pipeline.Modules.FTPDistribute;

public class FTPDistributeModule : IDistributeModule
{
    public string Name => "FTP";

    public Type StepType { get; } = typeof(FTPDistributeStep);

    public Type StepConfigType { get; } = typeof(FTPDistributeConfig);

    public string StepConfigName => "FTP Upload";
}