using System;
using Newtonsoft.Json;

namespace SeudoBuild.Modules.FTPDistribute
{
    public class FTPDistributeModule : IDistributeModule
    {
        public Type ArchiveStepType { get; } = typeof(FTPDistributeStep);

        public JsonConverter ConfigConverter { get; } = new EmailNotifyConfigConverter();

        public bool MatchesConfigType(DistributeStepConfig config)
        {
            return config is FTPDistributeConfig;
        }
    }
}
