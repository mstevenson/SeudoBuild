using System;
using Newtonsoft.Json;
using SeudoBuild.Data;

namespace SeudoBuild.Modules.FTPDistribute
{
    public class FTPDistributeModule : IDistributeModule
    {
        public Type StepType { get; } = typeof(FTPDistributeStep);

        public JsonConverter ConfigConverter { get; } = new EmailNotifyConfigConverter();

        public bool MatchesConfigType(StepConfig config)
        {
            return config is FTPDistributeConfig;
        }
    }
}
