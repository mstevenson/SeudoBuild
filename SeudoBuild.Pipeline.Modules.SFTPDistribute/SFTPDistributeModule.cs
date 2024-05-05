using System;

namespace SeudoBuild.Pipeline.Modules.SFTPDistribute
{
    public class SFTPDistributeModule : IDistributeModule
    {
        public string Name { get; } = "SFTP";

        public Type StepType { get; } = typeof(SFTPDistributeStep);

        public Type StepConfigType { get; } = typeof(SFTPDistributeConfig);

        public string StepConfigName { get; } = "SFTP Upload";
    }
}
