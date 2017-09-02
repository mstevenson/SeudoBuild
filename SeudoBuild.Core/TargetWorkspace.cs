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


    /// <summary>
    /// Manages the files and directory structure of a specific target within a project.
    /// </summary>
    public class TargetWorkspace : ITargetWorkspace
    {
        public static Platform RunningPlatform
        {
            get
            {
                // macOS is incorrectly detected as Unix. The solution is to check
                // for the presence of macOS root folders.
                // http://stackoverflow.com/questions/10138040/how-to-detect-properly-windows-linux-mac-operating-systems
                switch (Environment.OSVersion.Platform)
                {
                    case PlatformID.Unix:
                        if (Directory.Exists("/Applications")
                            & Directory.Exists("/System")
                            & Directory.Exists("/Users")
                            & Directory.Exists("/Volumes"))
                            return Platform.Mac;
                        else
                            return Platform.Linux;
                    case PlatformID.MacOSX:
                        return Platform.Mac;

                    default:
                        return Platform.Windows;
                }
            }
        }

        public static string StandardOutputPath
        {
            get
            {
                return RunningPlatform == Platform.Windows ? "CON" : "/dev/stdout";
            }
        }





        public enum DirectoryType
        {
        }

        public string GetDirectory(DirectoryType type)
        {
            throw new NotImplementedException();
        }

        public string CleanDirectory(DirectoryType type)
        {
            throw new NotImplementedException();
        }





        /// <summary>
        /// Contains project files downloaded from a version control system.
        /// </summary>
        public string SourceDirectory { get; private set; }

        /// <summary>
        /// Contains build output files.
        /// </summary>
        public string OutputDirectory { get; private set; }

        /// <summary>
        /// Contains build products that are packaged for distribution or archival.
        /// </summary>
        public string ArchivesDirectory { get; private set; }

        /// <summary>
        /// Contains detailed build logs for this target.
        /// </summary>
        public string LogsDirectory { get; private set; }

        public IMacros Macros { get; } = new Macros();

        public IFileSystem FileSystem { get; private set; }

        public TargetWorkspace(string projectDirectory, IFileSystem fileSystem)
        {
            SourceDirectory = $"{projectDirectory}/Workspace";
            Macros["working_directory"] = SourceDirectory;

            OutputDirectory = $"{projectDirectory}/Output";
            Macros["build_output_directory"] = OutputDirectory;

            ArchivesDirectory = $"{projectDirectory}/Archives";
            Macros["archives_directory"] = ArchivesDirectory;

            LogsDirectory = $"{projectDirectory}/Logs";
            Macros["logs_directory"] = LogsDirectory;

            this.FileSystem = fileSystem;
        }

        public void CreateSubDirectories()
        {
            if (!FileSystem.DirectoryExists(SourceDirectory))
            {
                FileSystem.CreateDirectory(SourceDirectory);
            }
            if (!FileSystem.DirectoryExists(OutputDirectory))
            {
                FileSystem.CreateDirectory(OutputDirectory);
            }
            if (!FileSystem.DirectoryExists(ArchivesDirectory))
            {
                FileSystem.CreateDirectory(ArchivesDirectory);
            }
            if (!FileSystem.DirectoryExists(LogsDirectory))
            {
                FileSystem.CreateDirectory(LogsDirectory);
            }
        }

        public void CleanSourceDirectory()
        {
            CleanDirectory(SourceDirectory);
        }

        public void CleanOutputDirectory()
        {
            CleanDirectory(OutputDirectory);
        }

        public void CleanArchivesDirectory()
        {
            CleanDirectory(ArchivesDirectory);
        }

        public void CleanLogsDirectory()
        {
            CleanDirectory(LogsDirectory);
        }

        void CleanDirectory (string directory)
        {
            if (!FileSystem.DirectoryExists(directory))
            {
                return;
            }

            FileSystem.DeleteDirectory(directory);
        }
    }
}
