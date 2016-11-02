using System.Collections.Generic;

namespace UnityBuildServer
{
    public enum Platform
    {
        Mac,
        Windows,
        Linux
    }

    public class UnityBuildSettings
    {
        public Platform TargetPlatform { get; set; }
        public string ExecutableName { get; set; }
        public string ExecuteMethod { get; set; }
        //public bool DevelopmentBuild { get; set; }
        //public string PreExportMethod { get; set; }
        //public string PostExportMethod { get; set; }
        //public List<string> CustomDefines { get; set; } = new List<string>();
        //public List<string> SceneList { get; set; } = new List<string>();
        //public bool BuildAssetBundles { get; set; }
        //public bool CopyAssetBundlesToStreamingAssets { get; set; }
    }
}
