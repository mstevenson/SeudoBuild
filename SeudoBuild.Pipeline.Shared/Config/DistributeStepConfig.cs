namespace SeudoBuild.Pipeline
{
    /// <inheritdoc />
    /// <summary>
    /// Configuration values for a Distribute pipeline step.
    /// </summary>
    public abstract class DistributeStepConfig : StepConfig
    {
        /// <summary>
        /// The name of the archive to distribute.
        /// </summary>
        public string ArchiveFileName { get; set; }
    }
}
