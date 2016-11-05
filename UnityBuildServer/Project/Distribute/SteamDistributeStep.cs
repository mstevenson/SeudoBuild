using System.Collections.Generic;

namespace UnityBuild
{
    public class SteamDistributeStep : DistributeStep
    {
        SteamDistributeConfig config;

        public SteamDistributeStep(SteamDistributeConfig config)
        {
            this.config = config;
        }

        public override string TypeName
        {
            get
            {
                return "Steam Upload";
            }
        }

        public override void Distribute(List<ArchiveInfo> archiveInfos, Workspace workspace)
        {
            // TODO
        }
    }
}
