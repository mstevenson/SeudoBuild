namespace SeudoCI.Pipeline;

/// <inheritdoc />
/// <summary>
/// Configuration values for a Notify pipeline step.
/// </summary>
public abstract class NotifyStepConfig : StepConfig
{
    /// <summary>
    /// When true the notify sequence will execute even if the previous pipeline
    /// sequence failed. This allows notification steps to report failures from
    /// earlier stages instead of being skipped.
    /// </summary>
    public bool RunOnFailure { get; set; }
}