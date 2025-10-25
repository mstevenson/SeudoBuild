namespace SeudoCI.Core.Tests;

[TestFixture]
public class ProjectWorkspaceTest
{
    [Test]
    public void GetDirectory_ReturnsDirRelativeToBaseDir()
    {
        var fileSystem = new MockFileSystem();
        var workspace = new ProjectWorkspace("/project", fileSystem);

        Assert.That(workspace.GetDirectory(ProjectDirectory.Project), Is.EqualTo("/project"));
        Assert.That(workspace.GetDirectory(ProjectDirectory.Targets), Is.EqualTo("/project/Targets"));
        Assert.That(workspace.GetDirectory(ProjectDirectory.Logs), Is.EqualTo("/project/Logs"));
    }

    [Test]
    public void CleanDirectory_DeletesDirectory()
    {
        var fileSystem = new MockFileSystem();
        var workspace = new ProjectWorkspace("/project", fileSystem);
        var targetsDirectory = workspace.GetDirectory(ProjectDirectory.Targets);

        fileSystem.CreateDirectory(targetsDirectory);
        Assume.That(fileSystem.DirectoryExists(targetsDirectory), Is.True);

        workspace.CleanDirectory(ProjectDirectory.Targets);

        Assert.That(fileSystem.DirectoryExists(targetsDirectory), Is.False);
    }

    [Test]
    public void InitializeDirectories_CreatesProjectDirectory()
    {
        var fileSystem = new MockFileSystem();
        var workspace = new ProjectWorkspace("/project", fileSystem);

        workspace.InitializeDirectories();

        Assert.That(fileSystem.DirectoryExists("/project"), Is.True);
    }

    [Test]
    public void InitializeDirectories_CreatesTargetsDirectory()
    {
        var fileSystem = new MockFileSystem();
        var workspace = new ProjectWorkspace("/project", fileSystem);

        workspace.InitializeDirectories();

        Assert.That(fileSystem.DirectoryExists("/project/Targets"), Is.True);
    }

    [Test]
    public void InitializeDirectories_CreatesLogsDirectory()
    {
        var fileSystem = new MockFileSystem();
        var workspace = new ProjectWorkspace("/project", fileSystem);

        workspace.InitializeDirectories();

        Assert.That(fileSystem.DirectoryExists("/project/Logs"), Is.True);
    }
}
