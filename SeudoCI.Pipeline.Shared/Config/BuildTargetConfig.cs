namespace SeudoCI.Pipeline;

using System.Collections.Generic;

/// <summary>
/// Configuration values for all pipeline steps associated with
/// a specific build target.
/// </summary>
public class BuildTargetConfig
{
    public string TargetName { get; set; }
    public VersionNumber Version = new VersionNumber();
    public List<SourceStepConfig> SourceSteps { get; set; } = [];
    public List<BuildStepConfig> BuildSteps { get; set; } = [];
    public List<ArchiveStepConfig> ArchiveSteps { get; set; } = [];
    public List<DistributeStepConfig> DistributeSteps { get; set; } = [];
    public List<NotifyStepConfig> NotifySteps { get; set; } = [];
}