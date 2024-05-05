namespace SeudoCI.Pipeline
{
    /// <inheritdoc />
    /// <summary>
    /// Results from creating an archive from a build product.
    /// </summary>
    public class ArchiveStepResults : PipelineStepResults
    {
        public string ArchiveFileName { get; set; }
    }
}
