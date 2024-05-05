using SeudoCI.Pipeline;

namespace SeudoCI.Agent
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

        /// <summary>
        /// Configuration settings for the build process.
        /// </summary>
        public ProjectConfig ProjectConfiguration { get; set; }

        /// <summary>
        /// The active target for the build process.
        /// </summary>
        public string TargetName { get; set; }

        /// <summary>
        /// The status of the build process for the current project and target.
        /// </summary>
        public Status BuildStatus { get; set; }
    }
}
