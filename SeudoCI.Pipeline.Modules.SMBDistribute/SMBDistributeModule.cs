namespace SeudoCI.Pipeline.Modules.SMBDistribute;

public class SMBDistributeModule : IDistributeModule
{
    public string Name => "SMB";

    public Type StepType { get; } = typeof(SMBDistributeStep);

    public Type StepConfigType { get; } = typeof(SMBDistributeConfig);

    public string StepConfigName => "SMB Transfer";
}