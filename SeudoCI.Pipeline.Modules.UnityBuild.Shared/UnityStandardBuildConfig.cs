namespace SeudoCI.Pipeline.Modules.UnityBuild
{
    /// <summary>
    /// Configuration values for a build pipeline step that performs a
    /// standard Unity build.
    /// </summary>
    public class UnityStandardBuildConfig : UnityBuildConfig
    {
        public override string Name { get; } = "Unity Standard Build";

        public string OutputName { get; set; } = "";

        /// <summary>
        /// If ExecuteStaticMethod is false, a standard build will be performed
        /// for this TargetPlatform.
        /// </summary>
        public UnityPlatform TargetPlatform { get; set; }
    }
}
