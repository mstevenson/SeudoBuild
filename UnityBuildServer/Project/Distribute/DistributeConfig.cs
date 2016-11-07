using System;
namespace UnityBuild
{
    public abstract class DistributeConfig
    {
        public abstract string Type { get; }

        /// <summary>
        /// The name of the archive to distribute.
        /// </summary>
        public string ArchiveFileName { get; set; }
    }
}
