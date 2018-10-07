using System;

namespace SeudoBuild.Core
{
    // Macros:
    // 
    // %project_name% -- the name for the entire project
    // %build_target_name% -- the specific target that was built
    // %app_version% -- version number as major.minor.patch
    // %build_date% -- the date that the build was completed
    // %commit_identifier% -- the current commit number or hash
    
    public class TargetWorkspace : ITargetWorkspace
    {
        private readonly string _baseDirectory;
        
        public IMacros Macros { get; } = new Macros();

        public IFileSystem FileSystem { get; }
        
        public TargetWorkspace(string baseDirectory, IFileSystem fileSystem)
        {
            _baseDirectory = baseDirectory;
            FileSystem = fileSystem;
            
            Macros["working_directory"] = GetDirectory(TargetDirectory.Source);
            Macros["build_output_directory"] = GetDirectory(TargetDirectory.Output);
            Macros["archives_directory"] = GetDirectory(TargetDirectory.Archives);
            Macros["logs_directory"] = GetDirectory(TargetDirectory.Logs);
        }
        
        public string GetDirectory(TargetDirectory directory)
        {
            switch (directory)
            {
                case TargetDirectory.Source:
                    return $"{_baseDirectory}/Workspace";
                case TargetDirectory.Output:
                    return $"{_baseDirectory}/Output";
                case TargetDirectory.Archives:
                    return $"{_baseDirectory}/Archives";
                case TargetDirectory.Logs:
                    return $"{_baseDirectory}/Logs";
                default:
                    throw new ArgumentOutOfRangeException(nameof(directory), directory, null);
            }
        }

        public void CleanDirectory(TargetDirectory directory)
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
            FileSystem.CreateDirectory(GetDirectory(TargetDirectory.Source));
            FileSystem.CreateDirectory(GetDirectory(TargetDirectory.Output));
            FileSystem.CreateDirectory(GetDirectory(TargetDirectory.Archives));
            FileSystem.CreateDirectory(GetDirectory(TargetDirectory.Logs));
        }
    }
}
