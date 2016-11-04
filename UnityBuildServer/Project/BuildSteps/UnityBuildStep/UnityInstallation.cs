using System;
namespace UnityBuildServer
{
    public class UnityInstallation
    {
        /// <summary>
        /// Version number of the installed executable that may be
        /// used by a Unity build step.
        /// </summary>
        public VersionNumber Version { get; set; }

        /// <summary>
        /// Local path to the Unity executable.
        /// </summary>
        public string Path { get; set; }
    }
}
