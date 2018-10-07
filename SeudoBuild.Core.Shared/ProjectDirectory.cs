namespace SeudoBuild.Core
{
    /// <summary>
    /// Directories in the top level of a project.
    /// </summary>
    public enum ProjectDirectory
    {
        /// <summary>
        /// Top-level directory for the project.
        /// </summary>
        Project,
        /// <summary>
        /// Directory containing individual build targets.
        /// </summary>
        Targets,
        /// <summary>
        /// Directory containing high-level logs for an entire project.
        /// </summary>
        Logs
    }
}