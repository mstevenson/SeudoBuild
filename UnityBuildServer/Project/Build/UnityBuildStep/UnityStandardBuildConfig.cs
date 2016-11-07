using System.Collections.Generic;

namespace UnityBuild
{
    /// <summary>
    /// Performs a standard Unity build, or executes an arbitrary static method in a Unity editor script.
    /// </summary>
    public class UnityStandardBuildConfig : BuildStepConfig
    {
        public override string Type { get; } = "Unity Standard Build";

        public string OutputName { get; set; }

        /// <summary>
        /// If ExecuteStaticMethod is false, a standard build will be performed
        /// for this TargetPlatform.
        /// </summary>
        public UnityPlatform TargetPlatform { get; set; }

        /// <summary>
        /// The installed Unity executable to build with.
        /// </summary>
        public VersionNumber UnityVersionNumber { get; set; }
    }
}
