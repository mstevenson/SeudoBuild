using System;
namespace UnityBuild
{
    public class FolderArchiveStepConfig : ArchiveStepConfig
    {
        public override string Type { get; } = "Folder";
        public string FolderName { get; set; }
    }
}
