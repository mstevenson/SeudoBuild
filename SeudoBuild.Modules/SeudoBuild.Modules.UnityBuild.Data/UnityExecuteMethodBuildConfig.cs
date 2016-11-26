namespace SeudoBuild.Pipeline.Modules.UnityBuild
{
    /// <summary>
    /// Executes an arbitrary static method in a Unity editor script.
    /// </summary>
    public class UnityExecuteMethodBuildConfig : UnityBuildConfig
    {
        public override string Type { get; } = "Unity Execute Method";

        /// <summary>
        /// A static method in the UnityEditor namespace matching this name will be executed.
        /// </summary>
        public string MethodName { get; set; } = "";
    }
}
