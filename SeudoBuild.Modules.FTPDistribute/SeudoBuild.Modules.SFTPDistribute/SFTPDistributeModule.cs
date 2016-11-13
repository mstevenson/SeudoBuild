using System;
using Newtonsoft.Json;

namespace SeudoBuild.Modules.SFTPDistribute
{
    public class SFTPDistributeModule : IDistributeModule
    {
        public Type ArchiveStepType { get; } = typeof(SFTPDistributeStep);

        public JsonConverter ConfigConverter { get; } = new SFTPDistributeConfigConverter();

        public bool MatchesConfigType(DistributeStepConfig config)
        {
            return config is SFTPDistributeConfig;
        }
    }
}
