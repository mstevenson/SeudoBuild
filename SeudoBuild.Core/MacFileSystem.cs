namespace SeudoBuild
{
    /// <summary>
    /// Default Mac filesystem for standalone apps.
    /// </summary>
    public class MacFileSystem : WindowsFileSystem
    {
        public override string StandardOutputPath { get; } = "/dev/stdout";
        
        public override string DocumentsPath => base.DocumentsPath + "/Documents";
    }
}