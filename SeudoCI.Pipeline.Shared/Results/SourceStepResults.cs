namespace SeudoCI.Pipeline;

/// <inheritdoc />
/// <summary>
/// Results from retrieving project files from a repository.
/// </summary>
public class SourceStepResults : PipelineStepResults
{
    public string CommitIdentifier { get; set; } = string.Empty;
}