using System;
namespace SeudoBuild.Agent
{
    public class BuildResult
    {
        public enum Status
        {
            Queued,
            Complete,
            Failed,
            Cancelled
        }

        public int Id { get; set; }
        public string ProjectName { get; set; }
        public string TargetName { get; set; }
        public Status BuildStatus { get; set; }
    }
}
