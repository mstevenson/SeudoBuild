using SeudoBuild.Pipeline;

namespace SeudoBuild.Agent
{
    /// <summary>
    /// Describes a queued build.
    /// </summary>
    public class BuildResult
    {
        public enum Status
        {
            Queued,
            Complete,
            Failed,
            Cancelled
        }

        /// <summary>
        /// Unique build process identifier.
        /// </summary>
        public int Id { get; set; }

        public ProjectConfig ProjectConfiguration { get; set; }
        public string TargetName { get; set; }
        public Status BuildStatus { get; set; }
    }
}
