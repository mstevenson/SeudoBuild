namespace SeudoCI.Pipeline.Modules.UnityBuild;

using System;
using System.Collections.Generic;
using System.IO;
using Core;

public class UnityInstallation
{
    private const string WindowsEditorPathEnv = "UNITY_WINDOWS_EDITOR_PATH";
    private const string WindowsRootEnv = "UNITY_WINDOWS_INSTALLATION_ROOT";
    private const string LinuxEditorPathEnv = "UNITY_LINUX_EDITOR_PATH";
    private const string LinuxRootEnv = "UNITY_LINUX_INSTALLATION_ROOT";

    /// <summary>
    /// Version number of the installed executable that may be
    /// used by a Unity build step.
    /// </summary>
    public VersionNumber? Version { get; set; }

    /// <summary>
    /// Full path to the Unity installation folder.
    /// </summary>
    /// <value>The folder.</value>
    public string? Folder { get; set; }

    /// <summary>
    /// Full path to the Unity executable.
    /// </summary>
    public string? ExePath { get; set; }

    /// <summary>
    /// Finds a Unity executable inside an installation folder with
    /// the folder name ending with the given version number.
    /// Example:  /Applications/Unity 5.3.4f2/Unity.app
    /// </summary>
    public static UnityInstallation FindUnityInstallation(VersionNumber? version, Platform platform)
    {
        switch (platform)
        {
            case Platform.Mac:
                // find the installation folder
                string installationPath = "/Applications/";
                string versionString = version != null ? version.ToString() : string.Empty;
                bool foundInstallationFolder = false;
                foreach (var dir in Directory.GetDirectories(installationPath))
                {
                    string filename = Path.GetFileName(dir);
                    if (filename.StartsWith("Unity", StringComparison.OrdinalIgnoreCase) && filename.EndsWith(versionString, StringComparison.OrdinalIgnoreCase))
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
                    if (filename.StartsWith("Unity", StringComparison.OrdinalIgnoreCase) && !filename.StartsWith("Unity Bug Reporter", StringComparison.OrdinalIgnoreCase) && filename.EndsWith(".app", StringComparison.OrdinalIgnoreCase))
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
            case Platform.Windows:
                return FindDesktopInstallation(version, platform, WindowsEditorPathEnv, WindowsRootEnv, "Unity.exe", GetWindowsRoots());
            case Platform.Linux:
                return FindDesktopInstallation(version, platform, LinuxEditorPathEnv, LinuxRootEnv, "Unity", GetLinuxRoots());
            default:
                throw new PlatformNotSupportedException($"{platform} platform is not supported for Unity builds");
        }
    }

    private static UnityInstallation FindDesktopInstallation(
        VersionNumber? version,
        Platform platform,
        string directOverrideEnv,
        string rootOverrideEnv,
        string exeFileName,
        IEnumerable<string> knownRoots)
    {
        var attemptedPaths = new List<string>();
        var seenPaths = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        var directOverride = Environment.GetEnvironmentVariable(directOverrideEnv);
        var directInstallation = TryResolveCandidate(directOverride, version, exeFileName, attemptedPaths, seenPaths);
        if (directInstallation != null)
        {
            return directInstallation;
        }

        var overrideRoot = Environment.GetEnvironmentVariable(rootOverrideEnv);
        if (!string.IsNullOrWhiteSpace(overrideRoot))
        {
            var installation = TryResolveFromRoot(overrideRoot, version, exeFileName, attemptedPaths, seenPaths);
            if (installation != null)
            {
                return installation;
            }
        }

        var seenRoots = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        foreach (var root in knownRoots)
        {
            if (string.IsNullOrWhiteSpace(root))
            {
                continue;
            }

            var expandedRoot = ExpandPath(root);
            if (!seenRoots.Add(expandedRoot))
            {
                continue;
            }

            var installation = TryResolveFromRoot(expandedRoot, version, exeFileName, attemptedPaths, seenPaths);
            if (installation != null)
            {
                return installation;
            }
        }

        var searched = attemptedPaths.Count == 0 ? "none" : string.Join(", ", attemptedPaths);
        throw new Exception($"Could not find Unity installation for {platform}. Paths searched: {searched}");
    }

    private static UnityInstallation? TryResolveFromRoot(
        string root,
        VersionNumber? version,
        string exeFileName,
        ICollection<string> attemptedPaths,
        ISet<string> seenPaths)
    {
        var installation = TryResolveCandidate(root, version, exeFileName, attemptedPaths, seenPaths);
        if (installation != null)
        {
            return installation;
        }

        var expandedRoot = ExpandPath(root);
        if (!Directory.Exists(expandedRoot))
        {
            return null;
        }

        var versionString = version?.ToString();
        if (!string.IsNullOrEmpty(versionString))
        {
            foreach (var folderName in GetVersionCandidateFolders(versionString))
            {
                var candidatePath = Path.Combine(expandedRoot, folderName);
                installation = TryResolveCandidate(candidatePath, version, exeFileName, attemptedPaths, seenPaths);
                if (installation != null)
                {
                    return installation;
                }
            }
        }

        foreach (var directory in SafeEnumerateDirectories(expandedRoot))
        {
            installation = TryResolveCandidate(directory, version, exeFileName, attemptedPaths, seenPaths);
            if (installation != null)
            {
                return installation;
            }
        }

        return null;
    }

    private static UnityInstallation? TryResolveCandidate(
        string? candidate,
        VersionNumber? version,
        string exeFileName,
        ICollection<string> attemptedPaths,
        ISet<string> seenPaths)
    {
        if (string.IsNullOrWhiteSpace(candidate))
        {
            return null;
        }

        var expanded = ExpandPath(candidate);
        if (string.IsNullOrWhiteSpace(expanded))
        {
            return null;
        }

        AddAttemptedPath(attemptedPaths, seenPaths, expanded);

        if (File.Exists(expanded))
        {
            return CreateInstallation(expanded, version);
        }

        if (!Directory.Exists(expanded))
        {
            return null;
        }

        var directExe = Path.Combine(expanded, exeFileName);
        AddAttemptedPath(attemptedPaths, seenPaths, directExe);
        if (File.Exists(directExe))
        {
            return CreateInstallation(directExe, version);
        }

        var editorExe = Path.Combine(expanded, "Editor", exeFileName);
        AddAttemptedPath(attemptedPaths, seenPaths, editorExe);
        if (File.Exists(editorExe))
        {
            return CreateInstallation(editorExe, version);
        }

        return null;
    }

    private static void AddAttemptedPath(ICollection<string> attemptedPaths, ISet<string> seenPaths, string path)
    {
        if (seenPaths.Add(path))
        {
            attemptedPaths.Add(path);
        }
    }

    private static UnityInstallation CreateInstallation(string exePath, VersionNumber? version)
    {
        var folder = Path.GetDirectoryName(exePath) ?? exePath;
        return new UnityInstallation
        {
            Version = version,
            Folder = folder,
            ExePath = exePath
        };
    }

    private static IEnumerable<string> SafeEnumerateDirectories(string path)
    {
        if (!Directory.Exists(path))
        {
            yield break;
        }

        string[] directories;

        try
        {
            directories = Directory.GetDirectories(path);
        }
        catch (IOException)
        {
            yield break;
        }
        catch (UnauthorizedAccessException)
        {
            yield break;
        }

        foreach (var directory in directories)
        {
            yield return directory;
        }
    }

    private static IEnumerable<string> GetVersionCandidateFolders(string versionString)
    {
        var seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        foreach (var candidate in new[]
                 {
                     versionString,
                     $"Unity {versionString}",
                     $"Unity_{versionString}",
                     $"Unity-{versionString}",
                     $"Unity{versionString}"
                 })
        {
            if (seen.Add(candidate))
            {
                yield return candidate;
            }
        }
    }

    private static string ExpandPath(string path)
    {
        if (string.IsNullOrWhiteSpace(path))
        {
            return path;
        }

        var expanded = Environment.ExpandEnvironmentVariables(path);
        if (expanded.StartsWith("~", StringComparison.Ordinal))
        {
            var home = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
            if (!string.IsNullOrEmpty(home))
            {
                if (expanded.Length == 1)
                {
                    expanded = home;
                }
                else if (expanded.Length > 1 && (expanded[1] == '/' || expanded[1] == '\\'))
                {
                    var remainder = expanded.Substring(2);
                    expanded = Path.Combine(home, remainder);
                }
            }
        }

        return expanded;
    }

    private static IEnumerable<string> GetWindowsRoots()
    {
        var roots = new List<string>
        {
            @"C:\\Program Files\\Unity",
            @"C:\\Program Files\\Unity\\Hub\\Editor",
            @"C:\\Program Files\\Unity Hub\\Editor",
            @"C:\\Program Files (x86)\\Unity",
            @"C:\\Program Files (x86)\\Unity\\Hub\\Editor",
            @"C:\\Program Files (x86)\\Unity Hub\\Editor"
        };

        var programFiles = Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles);
        if (!string.IsNullOrWhiteSpace(programFiles))
        {
            roots.Add(Path.Combine(programFiles, "Unity"));
            roots.Add(Path.Combine(programFiles, "Unity", "Hub", "Editor"));
            roots.Add(Path.Combine(programFiles, "Unity Hub", "Editor"));
        }

        var programFilesX86 = Environment.GetFolderPath(Environment.SpecialFolder.ProgramFilesX86);
        if (!string.IsNullOrWhiteSpace(programFilesX86))
        {
            roots.Add(Path.Combine(programFilesX86, "Unity"));
            roots.Add(Path.Combine(programFilesX86, "Unity", "Hub", "Editor"));
            roots.Add(Path.Combine(programFilesX86, "Unity Hub", "Editor"));
        }

        var localAppData = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
        if (!string.IsNullOrWhiteSpace(localAppData))
        {
            roots.Add(Path.Combine(localAppData, "Programs", "Unity"));
            roots.Add(Path.Combine(localAppData, "Unity", "Hub", "Editor"));
            roots.Add(Path.Combine(localAppData, "UnityHub", "Editor"));
        }

        return roots;
    }

    private static IEnumerable<string> GetLinuxRoots()
    {
        var roots = new List<string>
        {
            "/opt/Unity",
            "/opt/Unity/Hub/Editor",
            "/opt/unity",
            "/opt/unity/Hub/Editor",
            "/opt/unityhub/Editor",
            "/usr/share/unity3d",
            "/usr/local/share/unity3d",
            "~/Unity",
            "~/Unity/Hub/Editor",
            "~/UnityHub/Editor",
            "~/.local/share/Unity",
            "~/.local/share/UnityHub/Editor",
            "~/.local/share/unity3d"
        };

        var home = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
        if (!string.IsNullOrWhiteSpace(home))
        {
            roots.Add(Path.Combine(home, "Unity"));
            roots.Add(Path.Combine(home, "Unity", "Hub", "Editor"));
            roots.Add(Path.Combine(home, "UnityHub", "Editor"));
            roots.Add(Path.Combine(home, ".local", "share", "Unity"));
            roots.Add(Path.Combine(home, ".local", "share", "UnityHub", "Editor"));
            roots.Add(Path.Combine(home, ".local", "share", "unity3d"));
        }

        return roots;
    }
}
