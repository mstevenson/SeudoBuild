namespace SeudoCI.Pipeline.Tests;

using System;
using System.IO;
using Core;
using SeudoCI.Pipeline.Modules.UnityBuild;
using SeudoCI.Pipeline;

[TestFixture]
public class UnityInstallationPlatformTests
{
    [Test]
    public void FindUnityInstallation_Windows_UsesEnvironmentRootOverride()
    {
        var version = new VersionNumber { Major = 2021, Minor = 3, Patch = 5, Build = "f1" };
        var layout = CreateUnityLayout("windows-layout", "Unity.exe", version);

        Environment.SetEnvironmentVariable("UNITY_WINDOWS_EDITOR_PATH", null);
        Environment.SetEnvironmentVariable("UNITY_WINDOWS_INSTALLATION_ROOT", layout.Root);

        try
        {
            var installation = UnityInstallation.FindUnityInstallation(version, Platform.Windows);

            Assert.That(installation, Is.Not.Null);
            Assert.That(installation.ExePath, Is.EqualTo(layout.ExePath));
            Assert.That(installation.Folder, Is.EqualTo(Path.GetDirectoryName(layout.ExePath)));
            Assert.That(installation.Version, Is.EqualTo(version));
        }
        finally
        {
            Environment.SetEnvironmentVariable("UNITY_WINDOWS_INSTALLATION_ROOT", null);
            Environment.SetEnvironmentVariable("UNITY_WINDOWS_EDITOR_PATH", null);
            CleanupLayout(layout.Root);
        }
    }

    [Test]
    public void FindUnityInstallation_Linux_UsesEnvironmentRootOverride()
    {
        var version = new VersionNumber { Major = 2022, Minor = 1, Patch = 0, Build = "f1" };
        var layout = CreateUnityLayout("linux-layout", "Unity", version);

        Environment.SetEnvironmentVariable("UNITY_LINUX_EDITOR_PATH", null);
        Environment.SetEnvironmentVariable("UNITY_LINUX_INSTALLATION_ROOT", layout.Root);

        try
        {
            var installation = UnityInstallation.FindUnityInstallation(version, Platform.Linux);

            Assert.That(installation, Is.Not.Null);
            Assert.That(installation.ExePath, Is.EqualTo(layout.ExePath));
            Assert.That(installation.Folder, Is.EqualTo(Path.GetDirectoryName(layout.ExePath)));
            Assert.That(installation.Version, Is.EqualTo(version));
        }
        finally
        {
            Environment.SetEnvironmentVariable("UNITY_LINUX_INSTALLATION_ROOT", null);
            Environment.SetEnvironmentVariable("UNITY_LINUX_EDITOR_PATH", null);
            CleanupLayout(layout.Root);
        }
    }

    private static (string Root, string ExePath) CreateUnityLayout(string name, string exeFileName, VersionNumber version)
    {
        var baseDirectory = Path.Combine(TestContext.CurrentContext.WorkDirectory, name);
        CleanupLayout(baseDirectory);

        var editorDirectory = Path.Combine(baseDirectory, version.ToString(), "Editor");
        Directory.CreateDirectory(editorDirectory);

        var exePath = Path.Combine(editorDirectory, exeFileName);
        File.WriteAllText(exePath, string.Empty);

        return (baseDirectory, exePath);
    }

    private static void CleanupLayout(string path)
    {
        try
        {
            if (Directory.Exists(path))
            {
                Directory.Delete(path, true);
            }
        }
        catch (IOException)
        {
            // ignore cleanup failures in test tear down
        }
        catch (UnauthorizedAccessException)
        {
            // ignore cleanup failures in test tear down
        }
    }
}
