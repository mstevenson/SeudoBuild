using System;
using System.IO;

namespace SeudoBuild
{
    // Project directory structure example
    // 
    //  MyProject              // Project workspace
    //  ├─• ProjectConfig.json
    //  ├─• Logs/              // high-level logs pertaining to all build targets
    //  └─• Targets/
    //    ├─• Target_A/        // a target workspace
    //    │ ├─• Source/        // project source files to be built
    //    │ ├─• Output/        // build product
    //    │ ├─• Archives/      // zip files of build output
    //    │ └─• Logs/          // logs pertaining to the specific build target
    //    └─• Target_B/        // another workspace
    //      └─• Source/

    public class ProjectWorkspace
    {
        public string ProjectDirectory { get; private set; }

        /// <summary>
        /// Contains high level logs for an entire project.
        /// </summary>
        public string LogsDirectory { get; private set; }

        public string TargetsDirectory { get; private set; }

        public IFileSystem FileSystem { get; private set; }

        public ProjectWorkspace(string projectDirectory, IFileSystem fileSystem)
        {
            ProjectDirectory = projectDirectory;
            FileSystem = fileSystem;
            LogsDirectory = $"{ProjectDirectory}/Logs";
            TargetsDirectory = $"{ProjectDirectory}/Targets";
        }

        public void CreateSubdirectories()
        {
            if (!FileSystem.DirectoryExists(ProjectDirectory))
            {
                FileSystem.CreateDirectory(ProjectDirectory);
            }
            if (!FileSystem.DirectoryExists(LogsDirectory))
            {
                FileSystem.CreateDirectory(LogsDirectory);
            }
            if (!FileSystem.DirectoryExists(TargetsDirectory))
            {
                FileSystem.CreateDirectory(TargetsDirectory);
            }
        }

        public ITargetWorkspace CreateTarget(string targetName)
        {
            var targetWorkspace = new TargetWorkspace($"{TargetsDirectory}/{targetName.SanitizeFilename()}", FileSystem);
            targetWorkspace.CreateSubDirectories();
            return targetWorkspace;
        }

        public void CleanLogsDirectory()
        {
            CleanDirectory(LogsDirectory);
        }

        void CleanDirectory(string directory)
        {
            if (!FileSystem.DirectoryExists(directory))
            {
                return;
            }

            FileSystem.DeleteDirectory(directory);
        }
    }
}
