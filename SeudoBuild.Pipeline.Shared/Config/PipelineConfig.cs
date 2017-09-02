namespace SeudoBuild.Pipeline
{
    /// <summary>
    /// Configuration values for PipelineRunner.
    /// </summary>
    public class PipelineConfig
    {
        /// <summary>
        /// Base directory used by each pipeline step.
        /// </summary>
        public string OutputDirectory { get; set; }
    }
}
