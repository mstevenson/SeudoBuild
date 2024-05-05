using System;

namespace SeudoCI.Pipeline.Modules.SMBDistribute
{
    public class SMBDistributeModule : IDistributeModule
    {
        public string Name { get; } = "SMB";

        public Type StepType { get; } = typeof(SMBDistributeStep);

        public Type StepConfigType { get; } = typeof(SMBDistributeConfig);

        public string StepConfigName { get; } = "SMB Transfer";
    }
}
