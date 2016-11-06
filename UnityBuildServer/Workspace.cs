using System;
using System.IO;

namespace UnityBuild
{
    public class Workspace
    {
        public enum PlatformType
        {
            Mac,
            Windows,
            Linux
        }

        public PlatformType Platform
        {
            get
            {
                switch (Environment.OSVersion.Platform)
                {
                    case PlatformID.MacOSX:
                        return PlatformType.Mac;
                    case PlatformID.Win32NT:
                        return PlatformType.Windows;
                    case PlatformID.Unix:
                        return PlatformType.Linux;
                    default:
                        throw new PlatformNotSupportedException("The current platform is not supported.");
                }
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
                Replacements["working_directory"] = workingDirectory;
            }
        }
        string workingDirectory;

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
                Replacements["build_output_directory"] = buildOutputDirectory;
            }
        }
        string buildOutputDirectory;

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
                Replacements["archives_directory"] = archivesDirectory;
            }
        }
        string archivesDirectory;

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
                Replacements["logs_directory"] = logsDirectory;
            }
        }
        string logsDirectory;

        public TextReplacements Replacements { get; } = new TextReplacements();

        public Workspace(string projectDirectory)
        {
            WorkingDirectory = $"{projectDirectory}/Workspace";
            BuildOutputDirectory = $"{projectDirectory}/BuildOutput";
            ArchivesDirectory = $"{projectDirectory}/Archives";
            LogsDirectory = $"{projectDirectory}/Logs";
        }

        public void CreateSubDirectories()
        {
            if (!Directory.Exists(WorkingDirectory))
            {
                Directory.CreateDirectory(WorkingDirectory);
            }
            if (!Directory.Exists(BuildOutputDirectory))
            {
                Directory.CreateDirectory(BuildOutputDirectory);
            }
            if (!Directory.Exists(ArchivesDirectory))
            {
                Directory.CreateDirectory(ArchivesDirectory);
            }
            if (!Directory.Exists(LogsDirectory))
            {
                Directory.CreateDirectory(LogsDirectory);
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
            if (!Directory.Exists(directory))
            {
                return;
            }

            DirectoryInfo directoryInfo = new DirectoryInfo(directory);

            foreach (FileInfo file in directoryInfo.GetFiles())
            {
                file.Delete();
            }

            foreach (DirectoryInfo dir in directoryInfo.GetDirectories())
            {
                dir.Delete(true);
            }
        }
    }
}
