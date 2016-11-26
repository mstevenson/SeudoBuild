using System;
using System.IO;

namespace SeudoBuild
{
    // Macros:
    // 
    // %project_name% -- the name for the entire project
    // %build_target_name% -- the specific target that was built
    // %app_version% -- version number as major.minor.patch
    // %build_date% -- the date that the build was completed
    // %commit_identifier% -- the current commit number or hash

    public interface IWorkspace
    {
        /// <summary>
        /// Contains project files downloaded from a version control system.
        /// </summary>
        string WorkingDirectory { get; set; }

        /// <summary>
        /// Contains intermediate build files
        /// </summary>
        /// <value>The output directory.</value>
        string BuildOutputDirectory { get; set; }

        /// <summary>
        /// Contains products resulting from a build.
        /// </summary>
        string ArchivesDirectory { get; set; }

        /// <summary>
        /// Contains products resulting from a build.
        /// </summary>
        string LogsDirectory { get; set; }

        IMacros Macros { get; }

        IFileSystem FileSystem { get; }

        void CreateSubDirectories();

        void CleanWorkingDirectory();

        void CleanBuildOutputDirectory();

        void CleanArchivesDirectory();

        void CleanLogsDirectory();
    }
}
