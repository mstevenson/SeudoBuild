namespace SeudoCI.Pipeline.Modules.UnityBuild;

/// <inheritdoc />
/// <summary>
/// Configuration values for a build pipeline step that executes an arbitrary
/// static method in a Unity editor script.
/// </summary>
public class UnityExecuteMethodBuildConfig : UnityBuildConfig
{
    public override string Name => "Unity Execute Method";

    /// <summary>
    /// A static method in the UnityEditor namespace matching this name will be executed.
    /// </summary>
    public string MethodName { get; } = "";
}