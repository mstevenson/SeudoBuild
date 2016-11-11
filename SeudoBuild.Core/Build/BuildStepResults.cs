using System;
namespace SeudoBuild
{
    public class BuildStepResults : IPipelineStepResults
    {
        public string ProjectName { get; set; } = string.Empty;
        public string BuildTargetName { get; set; } = string.Empty;
        //public BuildCompletionStatus Status { get; set; }
        public DateTime BuildDate { get; set; } = DateTime.Now;
        public TimeSpan BuildDuration { get; set; } = new TimeSpan();
        public string CommitIdentifier { get; set; } = string.Empty;
        public VersionNumber AppVersion { get; set; } = new VersionNumber();
    }
}
