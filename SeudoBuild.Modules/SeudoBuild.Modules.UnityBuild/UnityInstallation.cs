using System;
using System.IO;

namespace SeudoBuild.Pipeline.Modules.UnityBuild
{
    public class UnityInstallation
    {
        /// <summary>
        /// Version number of the installed executable that may be
        /// used by a Unity build step.
        /// </summary>
        public VersionNumber Version { get; set; }

        /// <summary>
        /// Full path to the Unity installation folder.
        /// </summary>
        /// <value>The folder.</value>
        public string Folder { get; set; }

        /// <summary>
        /// Full path to the Unity executable.
        /// </summary>
        public string ExePath { get; set; }

        /// <summary>
        /// Finds an Unity executable inside an installation folder with
        /// the folder name ending with the given version number.
        /// Example:  /Applications/Unity 5.3.4f2/Unity.app
        /// </summary>
        public static UnityInstallation FindUnityInstallation (VersionNumber version, IFileSystem fileSystem)
        {
            Platform platform = Workspace.RunningPlatform;

            switch (platform)
            {
                case Platform.Mac:
                    // find the installation folder
                    string installationPath = "/Applications/";
                    string versionString = version != null ? version.ToString() : "";
                    bool foundInstallationFolder = false;
                    foreach (var dir in Directory.GetDirectories(installationPath))
                    {
                        string filename = Path.GetFileName(dir);
                        if (filename.StartsWith("Unity") && filename.EndsWith(versionString))
                        {
                            foundInstallationFolder = true;
                            installationPath += Path.GetFileName(dir);
                            break;
                        }
                    }
                    if (!foundInstallationFolder)
                    {
                        throw new Exception("Could not find Unity installation folder: " + installationPath);
                    }

                    // Find the app bundle
                    bool foundAppBundle = false;
                    string appBundlePath = installationPath;
                    foreach (var dir in Directory.GetDirectories(installationPath))
                    {
                        string filename = Path.GetFileName(dir);
                        if (filename.StartsWith("Unity") && !filename.StartsWith("Unity Bug Reporter") && filename.EndsWith(".app"))
                        {
                            foundAppBundle = true;
                            appBundlePath += "/" + filename;
                            break;
                        }
                    }
                    if (!foundAppBundle)
                    {
                        throw new Exception("Could not find Unity application: " + appBundlePath);
                    }

                    string exePath = $"{appBundlePath}/Contents/MacOS/Unity";
                    var installation = new UnityInstallation { Version = version, Folder = installationPath, ExePath = exePath };
                    return installation;
                default:
                    throw new PlatformNotSupportedException($"{platform} platform is not supported for Unity builds");
            }
        }
    }
}
