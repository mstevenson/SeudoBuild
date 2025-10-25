namespace SeudoCI.Pipeline;

/// <summary>
/// Configuration values for a pipeline step.
/// </summary>
public abstract class StepConfig
{
    /// <summary>
    /// The name of the step type.
    /// </summary>
    public abstract string Name { get; }

    /// <summary>
    /// Serialization discriminator for YAML documents.
    /// </summary>
    public string Type => Name;
}