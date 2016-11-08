using System;
namespace SeudoBuild
{
    public abstract class DistributeStepConfig
    {
        public abstract string Type { get; }

        /// <summary>
        /// The name of the archive to distribute.
        /// </summary>
        public string ArchiveFileName { get; set; }
    }
}
