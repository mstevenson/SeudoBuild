using System;
namespace UnityBuild
{
    public class FolderArchiveConfig : ArchiveStepConfig
    {
        public override string Type { get; } = "Folder";
        public string FolderName { get; set; }
    }
}
