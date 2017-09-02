namespace SeudoBuild
{
    // Macros:
    // 
    // %project_name% -- the name for the entire project
    // %build_target_name% -- the specific target that was built
    // %app_version% -- version number as major.minor.patch
    // %build_date% -- the date that the build was completed
    // %commit_identifier% -- the current commit number or hash

    public interface ITargetWorkspace
    {
        /// <summary>
        /// Contains project files downloaded from a version control system.
        /// </summary>
        string SourceDirectory { get; }

        /// <summary>
        /// Contains build output files.
        /// </summary>
        string OutputDirectory { get; }

        /// <summary>
        /// Contains build products that are packaged for distribution or archival.
        /// </summary>
        string ArchivesDirectory { get; }

        /// <summary>
        /// Contains build logs.
        /// </summary>
        string LogsDirectory { get; }

        IMacros Macros { get; }

        IFileSystem FileSystem { get; }

        void CreateSubDirectories();

        void CleanSourceDirectory();

        void CleanOutputDirectory();

        void CleanArchivesDirectory();

        void CleanLogsDirectory();
    }
}
