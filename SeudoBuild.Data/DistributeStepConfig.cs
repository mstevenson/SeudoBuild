using System;
namespace SeudoBuild
{
    public abstract class DistributeStepConfig : StepConfig
    {
        /// <summary>
        /// The name of the archive to distribute.
        /// </summary>
        public string ArchiveFileName { get; set; }
    }
}
