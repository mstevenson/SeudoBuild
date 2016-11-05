using System;
namespace UnityBuild
{
    public abstract class DistributeConfig
    {
        public string Id { get; set; }

        /// <summary>
        /// The name of the archive to distribute.
        /// </summary>
        public string ArchiveFileName { get; set; }
    }
}
