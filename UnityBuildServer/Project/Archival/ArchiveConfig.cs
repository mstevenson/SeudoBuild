using System;
namespace UnityBuildServer
{
    public abstract class ArchiveConfig
    {
        public string Id { get; set; }

        public abstract void CreateArchive(BuildInfo buildInfo, Workspace workspace);
    }
}
