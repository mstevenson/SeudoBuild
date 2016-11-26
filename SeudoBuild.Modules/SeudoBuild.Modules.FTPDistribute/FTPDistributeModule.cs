using System;
using System.Collections.Generic;

namespace SeudoBuild.Pipeline.Modules.FTPDistribute
{
    public class FTPDistributeModule : IDistributeModule
    {
        public string Name { get; } = "FTP";

        public Type StepType { get; } = typeof(FTPDistributeStep);

        public Type StepConfigType { get; } = typeof(FTPDistributeConfig);

        public string StepConfigName { get; } = "FTP Upload";
    }
}
