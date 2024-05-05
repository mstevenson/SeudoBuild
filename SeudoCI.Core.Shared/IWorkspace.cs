namespace SeudoCI.Core
{
    // Macros:
    // 
    // %project_name% -- the name for the entire project
    // %build_target_name% -- the specific target that was built
    // %app_version% -- version number as major.minor.patch
    // %build_date% -- the date that the build was completed
    // %commit_identifier% -- the current commit number or hash

    public interface IWorkspace<in T>
    {
        IFileSystem FileSystem { get; }

        IMacros Macros { get; }
        
        string GetDirectory(T directory);

        void CleanDirectory(T directory);

        void InitializeDirectories();
    }
}
