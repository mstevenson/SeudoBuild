namespace SeudoCI.Pipeline;

/// <summary>
/// Results from the completion of a single pipeline step.
/// </summary>
public abstract class PipelineStepResults
{
    public bool IsSuccess { get; set; }
    public Exception Exception { get; set; }
}