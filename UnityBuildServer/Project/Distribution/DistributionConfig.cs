using System;
namespace UnityBuildServer
{
    public abstract class DistributionConfig
    {
        public string Id { get; set; }

        /// <summary>
        /// The name of the archive to distribute.
        /// </summary>
        public string Archive { get; set; }
    }
}
