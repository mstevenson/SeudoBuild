﻿namespace SeudoBuild.Pipeline.Modules.FolderArchive
{
    public class FolderArchiveConfig : ArchiveStepConfig
    {
        public override string Type { get; } = "Folder";
        public string FolderName { get; set; }
    }
}
