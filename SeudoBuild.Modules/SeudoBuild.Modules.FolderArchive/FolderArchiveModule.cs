using System;
using Newtonsoft.Json;
using SeudoBuild.Data;

namespace SeudoBuild.Modules.ZipArchive
{
    public class FolderArchiveModule : IArchiveModule
    {
        public Type StepType { get; } = typeof(FolderArchiveStep);

        public JsonConverter ConfigConverter { get; } = new FolderArchiveConfigConverter();

        public bool MatchesConfigType(StepConfig config)
        {
            return config is FolderArchiveConfig;
        }
    }
}
