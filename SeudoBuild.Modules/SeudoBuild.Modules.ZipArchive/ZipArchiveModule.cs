using System;
using Newtonsoft.Json;
using SeudoBuild.Data;

namespace SeudoBuild.Modules.ZipArchive
{
    public class ZipArchiveModule : IArchiveModule
    {
        public Type ArchiveStepType { get; } = typeof(ZipArchiveStep);

        public JsonConverter ConfigConverter { get; } = new ZipArchiveConfigConverter();

        public bool MatchesConfigType(ArchiveStepConfig config)
        {
            return config is ZipArchiveConfig;
        }
    }
}
