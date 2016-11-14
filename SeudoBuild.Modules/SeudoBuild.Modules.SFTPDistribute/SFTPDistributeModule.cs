using System;
using Newtonsoft.Json;

namespace SeudoBuild.Modules.SFTPDistribute
{
    public class SFTPDistributeModule : IDistributeModule
    {
        public string Name { get; } = "SFTP";

        public Type StepType { get; } = typeof(SFTPDistributeStep);

        public JsonConverter ConfigConverter { get; } = new SFTPDistributeConfigConverter();

        public bool CanReadConfig(StepConfig config)
        {
            return config is SFTPDistributeConfig;
        }
    }
}
