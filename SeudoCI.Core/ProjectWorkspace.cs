using System;

namespace SeudoCI.Core
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
    
    public class ProjectWorkspace : IProjectWorkspace
    {
        private readonly string _baseDirectory;
        
        public IFileSystem FileSystem { get; }
        
        public IMacros Macros { get; } = new Macros();

        public ProjectWorkspace(string projectDirectory, IFileSystem fileSystem)
        {
            FileSystem = fileSystem;
            _baseDirectory = projectDirectory;
        }

        public string GetDirectory(ProjectDirectory directory)
        {
            switch (directory)
            {
                case ProjectDirectory.Project:
                    return _baseDirectory;
                case ProjectDirectory.Targets:
                    return $"{_baseDirectory}/Targets";
                case ProjectDirectory.Logs:
                    return $"{_baseDirectory}/Logs";
                default:
                    throw new ArgumentOutOfRangeException(nameof(directory), directory, null);
            }
        }
        
        public void CleanDirectory(ProjectDirectory directory)
        {
            var dir = GetDirectory(directory);
            
            if (!FileSystem.DirectoryExists(dir))
            {
                return;
            }

            FileSystem.DeleteDirectory(dir);
        }

        public void InitializeDirectories()
        {
            FileSystem.CreateDirectory(GetDirectory(ProjectDirectory.Project));
            FileSystem.CreateDirectory(GetDirectory(ProjectDirectory.Targets));
            FileSystem.CreateDirectory(GetDirectory(ProjectDirectory.Logs));
        }

        public ITargetWorkspace CreateTarget(string targetName)
        {
            var targetsDirectory = GetDirectory(ProjectDirectory.Targets);
            var targetWorkspace = new TargetWorkspace($"{targetsDirectory}/{targetName.SanitizeFilename()}", FileSystem);
            targetWorkspace.InitializeDirectories();
            return targetWorkspace;
        }
    }
}
