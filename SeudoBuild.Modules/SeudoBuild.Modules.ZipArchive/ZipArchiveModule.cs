using System;

namespace SeudoBuild.Pipeline.Modules.ZipArchive
{
    public class ZipArchiveModule : IArchiveModule
    {
        public string Name { get; } = "Zip";

        public Type StepType { get; } = typeof(ZipArchiveStep);

        public Type StepConfigType { get; } = typeof(ZipArchiveConfig);

        public string StepConfigName { get; } = "Zip File";

    }
}
