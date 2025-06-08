using JetBrains.Annotations;

namespace SeudoCI.Pipeline.Modules.ZipArchive;

[UsedImplicitly]
public class ZipArchiveModule : IArchiveModule
{
    public string Name => "Zip";

    public Type StepType { get; } = typeof(ZipArchiveStep);

    public Type StepConfigType { get; } = typeof(ZipArchiveConfig);

    public string StepConfigName => "Zip File";
}