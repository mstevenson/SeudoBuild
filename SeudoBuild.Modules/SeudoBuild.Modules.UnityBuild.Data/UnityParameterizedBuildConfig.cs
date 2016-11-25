using System.Collections.Generic;

namespace SeudoBuild.Modules.UnityBuild
{
    public class UnityParameterizedBuildConfig : UnityBuildConfig
    {
        public override string Type { get; } = "Unity Parameterized Build";

        public UnityPlatform TargetPlatform { get; set; }
        public string ExecutableName { get; set; }
        public string PreBuildMethod { get; set; }
        public bool DevelopmentBuild { get; set; }
        public string PreExportMethod { get; set; }
        public string PostExportMethod { get; set; }
        public List<string> CustomDefines { get; set; } = new List<string>();
        public List<string> SceneList { get; set; } = new List<string>();
        public bool BuildAssetBundles { get; set; }
        public bool CopyAssetBundlesToStreamingAssets { get; set; }
    }
}
