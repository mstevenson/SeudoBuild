namespace SeudoCI.Pipeline.Modules.ZipArchive;

public class ZipArchiveModule : IArchiveModule
{
    public string Name => "Zip";

    public Type StepType { get; } = typeof(ZipArchiveStep);

    public Type StepConfigType { get; } = typeof(ZipArchiveConfig);

    public string StepConfigName => "Zip File";
}