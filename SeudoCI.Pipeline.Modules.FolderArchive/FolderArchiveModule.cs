using JetBrains.Annotations;

namespace SeudoCI.Pipeline.Modules.FolderArchive;

[UsedImplicitly]
public class FolderArchiveModule : IArchiveModule
{
    public string Name => "Folder";

    public Type StepType { get; } = typeof(FolderArchiveStep);

    public Type StepConfigType { get; } = typeof(FolderArchiveConfig);

    public string StepConfigName => "Folder";
}