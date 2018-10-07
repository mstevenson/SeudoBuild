namespace SeudoBuild.Core
{
    /// <summary>
    /// Directories in the top level of a TargetWorkspace folder contained within a project.
    /// </summary>
    public enum TargetDirectory
    {
        /// <summary>
        /// Directory containing project source files to build.
        /// </summary>
        Source,
        /// <summary>
        /// Directory containing build output files.
        /// </summary>
        Output,
        /// <summary>
        /// Directory containing build products that are packaged for distribution or archival.
        /// </summary>
        Archives,
        /// <summary>
        /// Directory containing target-specific build log files.
        /// </summary>
        Logs
    }
}