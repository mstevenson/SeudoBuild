using System;
using Newtonsoft.Json;

namespace SeudoBuild.Modules.FTPDistribute
{
    public class FTPDistributeModule : IDistributeModule
    {
        public string Name { get; } = "FTP";

        public Type StepType { get; } = typeof(FTPDistributeStep);

        public JsonConverter ConfigConverter { get; } = new EmailNotifyConfigConverter();

        public bool MatchesConfigType(StepConfig config)
        {
            return config is FTPDistributeConfig;
        }
    }
}
