using System;
using Newtonsoft.Json;

namespace SeudoBuild.Modules.ZipArchive
{
    public class FolderArchiveModule : IArchiveModule
    {
        public string Name { get; } = "Folder";

        public Type StepType { get; } = typeof(FolderArchiveStep);

        public JsonConverter ConfigConverter { get; } = new FolderArchiveConfigConverter();

        public bool CanReadConfig(StepConfig config)
        {
            return config is FolderArchiveConfig;
        }
    }
}
