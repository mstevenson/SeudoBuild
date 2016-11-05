using System;
namespace UnityBuild
{
    /// <summary>
    /// Executes an arbitrary static method in a Unity editor script.
    /// </summary>
    public class UnityExecuteMethodBuildConfig : BuildStepConfig
    {
        public override string Type
        {
            get
            {
                return "Unity Execute Method";
            }
        }

        /// <summary>
        /// The installed Unity executable to build with.
        /// </summary>
        public VersionNumber UnityVersionNumber { get; set; }

        /// <summary>
        /// A static method in the UnityEditor namespace matching this name will be executed.
        /// </summary>
        public string MethodName { get; set; }
    }
}
