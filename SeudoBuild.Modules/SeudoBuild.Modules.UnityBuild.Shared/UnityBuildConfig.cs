namespace SeudoBuild.Pipeline.Modules.UnityBuild
{
    public abstract class UnityBuildConfig : BuildStepConfig
    {
        /// <summary>
        /// The installed Unity executable to build with.
        /// </summary>
        public VersionNumber UnityVersionNumber { get; set; }

        /// <summary>
        /// A path relative to the working directory that contains a Unity project.
        /// </summary>
        public string SubDirectory { get; set; } = "";
    }
}
