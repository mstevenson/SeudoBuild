using System;
using System.Collections.Generic;
using Newtonsoft.Json;

namespace SeudoCI.Pipeline.Modules.ZipArchive
{
    public class ZipArchiveModule : IArchiveModule
    {
        public string Name { get; } = "Zip";

        public Type StepType { get; } = typeof(ZipArchiveStep);

        public Type StepConfigType { get; } = typeof(ZipArchiveConfig);

        public string StepConfigName { get; } = "Zip File";

    }
}
