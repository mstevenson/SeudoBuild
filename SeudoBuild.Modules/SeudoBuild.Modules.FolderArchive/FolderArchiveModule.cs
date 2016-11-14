using System;
using System.Collections.Generic;
using Newtonsoft.Json;

namespace SeudoBuild.Modules.ZipArchive
{
    public class FolderArchiveModule : IArchiveModule
    {
        public string Name { get; } = "Folder";

        public Type StepType { get; } = typeof(FolderArchiveStep);

        public Type StepConfigType { get; } = typeof(FolderArchiveConfig);

        public string StepConfigName { get; } = "Folder";
    }
}
