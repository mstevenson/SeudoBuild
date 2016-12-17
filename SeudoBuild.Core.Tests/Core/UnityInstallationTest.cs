using NUnit.Framework;
using SeudoBuild;

namespace SeudoBuild.Tests
{
    [TestFixture]
    public class UnityInstallationTest
    {
        [Test]
        public void FindUnityInstallation_UnityExists_ReturnsInstallation()
        {
            //var version = new VersionNumber { Major = 5, Minor = 3, Patch = 5, Build = "f1" };
            //var filesystem = new MockFileSystem();
            //// FIXME only works on Mac
            //filesystem.allFiles.Add("/Applications/Unity/Unity 5.3.5f1/Unity.app/Contents/MacOS/Unity", new MockFile());

            //UnityInstallation installation = UnityInstallation.FindUnityInstallation(version, filesystem);

            //Assert.That(installation, Is.Not.Null);
            //Assert.That(installation.Folder, Is.EqualTo("/Applications/Unity 5.3.5f1"));
        }
    }
}
