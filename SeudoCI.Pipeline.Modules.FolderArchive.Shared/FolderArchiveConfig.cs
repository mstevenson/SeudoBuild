namespace SeudoCI.Pipeline.Modules.FolderArchive;

/// <summary>
/// Configuration values for an archive pipeline step that copies a build product into a folder.
/// </summary>
public class FolderArchiveConfig : ArchiveStepConfig
{
    public override string Name => "Folder";
    public string FolderName { get; set; } = string.Empty;
}