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

    public class Workspace
    {
        public enum Platform
        {
            Mac,
            Windows,
            Linux
        }

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

        /// <summary>
        /// Contains project files downloaded from a version control system.
        /// </summary>
        public string WorkingDirectory
        {
            get
            {
                return workingDirectory;
            }
            set
            {
                workingDirectory = value;
                Macros["working_directory"] = workingDirectory;
            }
        }
        string workingDirectory = "";

        /// <summary>
        /// Contains intermediate build files
        /// </summary>
        /// <value>The output directory.</value>
        public string BuildOutputDirectory
        {
            get
            {
                return buildOutputDirectory;
            }
            set
            {
                buildOutputDirectory = value;
                Macros["build_output_directory"] = buildOutputDirectory;
            }
        }
        string buildOutputDirectory = "";

        /// <summary>
        /// Contains products resulting from a build.
        /// </summary>
        public string ArchivesDirectory
        {
            get
            {
                return archivesDirectory;
            }
            set
            {
                archivesDirectory = value;
                Macros["archives_directory"] = archivesDirectory;
            }
        }
        string archivesDirectory = "";

        /// <summary>
        /// Contains products resulting from a build.
        /// </summary>
        public string LogsDirectory
        {
            get
            {
                return logsDirectory;
            }
            set
            {
                logsDirectory = value;
                Macros["logs_directory"] = logsDirectory;
            }
        }
        string logsDirectory = "";

        public Macros Macros { get; } = new Macros();

        public IFileSystem FileSystem { get; private set; }

        public Workspace(string projectDirectory, IFileSystem fileSystem)
        {
            WorkingDirectory = $"{projectDirectory}/Workspace";
            BuildOutputDirectory = $"{projectDirectory}/Output";
            ArchivesDirectory = $"{projectDirectory}/Archives";
            LogsDirectory = $"{projectDirectory}/Logs";
            this.FileSystem = fileSystem;
        }

        public void CreateSubDirectories()
        {
            if (!FileSystem.DirectoryExists(WorkingDirectory))
            {
                FileSystem.CreateDirectory(WorkingDirectory);
            }
            if (!FileSystem.DirectoryExists(BuildOutputDirectory))
            {
                FileSystem.CreateDirectory(BuildOutputDirectory);
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

        public void CleanWorkingDirectory()
        {
            CleanDirectory(WorkingDirectory);
        }

        public void CleanBuildOutputDirectory()
        {
            CleanDirectory(BuildOutputDirectory);
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
