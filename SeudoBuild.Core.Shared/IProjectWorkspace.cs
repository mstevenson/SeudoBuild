namespace SeudoBuild
{
    public interface IProjectWorkspace
    {
        string ProjectDirectory { get; }
        
        /// <summary>
        /// Contains high level logs for an entire project.
        /// </summary>
        string LogsDirectory { get; }
        
        IFileSystem FileSystem { get; }
        
        void CreateSubdirectories();
        
        ITargetWorkspace CreateTarget(string targetName);

        void CleanLogsDirectory();
    }
}
