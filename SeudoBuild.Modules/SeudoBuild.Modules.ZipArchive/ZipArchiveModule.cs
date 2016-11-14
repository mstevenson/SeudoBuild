using System;
using Newtonsoft.Json;

namespace SeudoBuild.Modules.ZipArchive
{
    public class ZipArchiveModule : IArchiveModule
    {
        public string Name { get; } = "Zip";

        public Type StepType { get; } = typeof(ZipArchiveStep);

        public JsonConverter ConfigConverter { get; } = new ZipArchiveConfigConverter();

        public bool CanReadConfig(StepConfig config)
        {
            return config is ZipArchiveConfig;
        }
    }
}
