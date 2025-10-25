namespace SeudoCI.Pipeline;

/// <summary>
/// Configuration values for an entire project that may contain one or more
/// build targets.
/// </summary>
public class ProjectConfig
{
    public string ProjectName { get; set; } = string.Empty;
        
    public List<BuildTargetConfig> BuildTargets { get; set; } = [];
}