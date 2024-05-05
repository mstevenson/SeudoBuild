namespace SeudoCI.Pipeline.Modules.ShellBuild
{
    /// <summary>
    /// Configuration values for a build pipeline step that runs a shell script.
    /// </summary>
    public class ShellBuildStepConfig : BuildStepConfig
    {
        public override string Name { get; } = "Shell Script";

        public string Command { get; set; }
    }
}
