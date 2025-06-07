namespace SeudoCI.Pipeline.Modules.UnityBuild;

/// <summary>
/// Configuration values for a build pipeline step that invokes a
/// SeudoCI-specific editor build script in a Unity project.
/// </summary>
public class UnityParameterizedBuildConfig : UnityBuildConfig
{
    public override string Name { get; } = "Unity Parameterized Build";

    public UnityPlatform TargetPlatform { get; set; }
    public string ExecutableName { get; set; } = string.Empty;
    public string PreBuildMethod { get; set; } = string.Empty;
    public bool DevelopmentBuild { get; set; }
    public string PreExportMethod { get; set; } = string.Empty;
    public string PostExportMethod { get; set; } = string.Empty;
    public List<string> CustomDefines { get; set; } = new List<string>();
    public List<string> SceneList { get; set; } = new List<string>();
    public bool BuildAssetBundles { get; set; }
    public bool CopyAssetBundlesToStreamingAssets { get; set; }
}