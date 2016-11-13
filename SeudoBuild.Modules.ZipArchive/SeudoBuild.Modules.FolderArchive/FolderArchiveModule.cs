using System;
using Newtonsoft.Json;

namespace SeudoBuild.Modules.ZipArchive
{
    public class FolderArchiveModule : IArchiveModule
    {
        public Type ArchiveStepType { get; } = typeof(FolderArchiveStep);

        public JsonConverter ConfigConverter { get; } = new FolderArchiveConfigConverter();

        public bool MatchesConfigType(ArchiveStepConfig config)
        {
            return config is FolderArchiveConfig;
        }
    }
}
