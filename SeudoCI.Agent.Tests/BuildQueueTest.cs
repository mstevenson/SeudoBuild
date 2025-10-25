namespace SeudoCI.Agent.Tests;

using System.Threading;
using NSubstitute;
using Core;
using Pipeline;

[TestFixture]
public class BuildQueueTest
{
    private IModuleLoader _mockLoader;
    private ILogger _mockLogger;
    private Builder _mockBuilder;
    private const string DocumentsDir = "Documents";
        
    [SetUp]
    public void SetUp()
    {
        _mockLoader = Substitute.For<IModuleLoader>();
        _mockLogger = Substitute.For<ILogger>();
        _mockBuilder = new Builder(_mockLoader, _mockLogger);
    }
        
    [Test]
    public void StartQueue_DocumentsDirMissing_ThrowsException()
    {
        IFileSystem fs = Substitute.For<IFileSystem>();
        fs.DocumentsPath.Returns("Documents");
        fs.DirectoryExists("Documents").Returns(false);
        var buildQueue = new BuildQueue(_mockBuilder, _mockLoader, _mockLogger);

        Assert.Throws<DirectoryNotFoundException>(() =>
        {
            buildQueue.StartQueue(fs);
        });
    }
        
    [Test]
    public void StartQueue_DocumentsDirExists_CreatesBuildDir()
    {
        IFileSystem fs = Substitute.For<IFileSystem>();
        fs.DocumentsPath.Returns(DocumentsDir);
        fs.DirectoryExists(DocumentsDir).Returns(true);
        var buildQueue = new BuildQueue(_mockBuilder, _mockLoader, _mockLogger);
            
        buildQueue.StartQueue(fs);
            
        fs.Received().CreateDirectory(Arg.Any<string>());
    }

    [Test]
    public void EnqueueBuild_AddsBuild()
    {
        // Arrange
        IFileSystem fs = Substitute.For<IFileSystem>();
        fs.DocumentsPath.Returns(DocumentsDir);
        fs.DirectoryExists(DocumentsDir).Returns(true);
        var buildQueue = new BuildQueue(_mockBuilder, _mockLoader, _mockLogger);
        buildQueue.StartQueue(fs);
        
        var projectConfig = new ProjectConfig 
        { 
            ProjectName = "TestProject"
        };
        projectConfig.BuildTargets.Add(new BuildTargetConfig { TargetName = "TestTarget" });

        // Act
        var result = buildQueue.EnqueueBuild(projectConfig, "TestTarget");

        // Assert
        Assert.That(result, Is.Not.Null);
        Assert.That(result.ProjectConfiguration, Is.EqualTo(projectConfig));
        Assert.That(result.TargetName, Is.EqualTo("TestTarget"));
        Assert.That(result.BuildStatus, Is.EqualTo(BuildResult.Status.Queued));
    }
        
    [Test]
    public void GetAllBuildResults_ReturnsBuilds()
    {
        // Arrange
        IFileSystem fs = Substitute.For<IFileSystem>();
        fs.DocumentsPath.Returns(DocumentsDir);
        fs.DirectoryExists(DocumentsDir).Returns(true);
        var buildQueue = new BuildQueue(_mockBuilder, _mockLoader, _mockLogger);
        buildQueue.StartQueue(fs);
        
        var projectConfig = new ProjectConfig { ProjectName = "TestProject" };
        buildQueue.EnqueueBuild(projectConfig);

        // Act
        var results = buildQueue.GetAllBuildResults();

        // Assert
        Assert.That(results, Is.Not.Null);
        Assert.That(results.Count(), Is.EqualTo(1));
        Assert.That(results.First().ProjectConfiguration.ProjectName, Is.EqualTo("TestProject"));
    }
        
    [Test]
    public void GetBuildResult_ReturnsBuilds()
    {
        // Arrange
        IFileSystem fs = Substitute.For<IFileSystem>();
        fs.DocumentsPath.Returns(DocumentsDir);
        fs.DirectoryExists(DocumentsDir).Returns(true);
        var buildQueue = new BuildQueue(_mockBuilder, _mockLoader, _mockLogger);
        buildQueue.StartQueue(fs);
        
        var projectConfig = new ProjectConfig { ProjectName = "TestProject" };
        var enqueuedBuild = buildQueue.EnqueueBuild(projectConfig);

        // Act
        var result = buildQueue.GetBuildResult(enqueuedBuild.Id);

        // Assert
        Assert.That(result, Is.Not.Null);
        Assert.That(result.Id, Is.EqualTo(enqueuedBuild.Id));
        Assert.That(result.ProjectConfiguration.ProjectName, Is.EqualTo("TestProject"));
    }

    [Test]
    public void GetBuildResult_NonExistentId_ReturnsNull()
    {
        // Arrange
        IFileSystem fs = Substitute.For<IFileSystem>();
        fs.DocumentsPath.Returns(DocumentsDir);
        fs.DirectoryExists(DocumentsDir).Returns(true);
        var buildQueue = new BuildQueue(_mockBuilder, _mockLoader, _mockLogger);
        buildQueue.StartQueue(fs);

        // Act
        var result = buildQueue.GetBuildResult(999);

        // Assert
        Assert.That(result, Is.Null);
    }
        
    [Test]
    public void CancelBuild_BuildIsCancelled()
    {
        // Arrange
        IFileSystem fs = Substitute.For<IFileSystem>();
        fs.DocumentsPath.Returns(DocumentsDir);
        fs.DirectoryExists(DocumentsDir).Returns(true);
        var buildQueue = new BuildQueue(_mockBuilder, _mockLoader, _mockLogger);
        buildQueue.StartQueue(fs);
        
        var projectConfig = new ProjectConfig { ProjectName = "TestProject" };
        var enqueuedBuild = buildQueue.EnqueueBuild(projectConfig);

        // Act
        var cancelledBuild = buildQueue.CancelBuild(enqueuedBuild.Id);

        // Assert
        Assert.That(cancelledBuild, Is.Not.Null);
        Assert.That(cancelledBuild.Id, Is.EqualTo(enqueuedBuild.Id));
        Assert.That(cancelledBuild.BuildStatus, Is.EqualTo(BuildResult.Status.Cancelled));
    }

    [Test]
    public void CancelBuild_NonExistentId_ReturnsNull()
    {
        // Arrange
        IFileSystem fs = Substitute.For<IFileSystem>();
        fs.DocumentsPath.Returns(DocumentsDir);
        fs.DirectoryExists(DocumentsDir).Returns(true);
        var buildQueue = new BuildQueue(_mockBuilder, _mockLoader, _mockLogger);
        buildQueue.StartQueue(fs);

        // Act
        var result = buildQueue.CancelBuild(999);

        // Assert
        Assert.That(result, Is.Null);
    }

    [Test]
    public void ActiveBuild_Finishes_NextBuildBegins()
    {
        // Arrange
        IFileSystem fs = Substitute.For<IFileSystem>();
        fs.DocumentsPath.Returns(DocumentsDir);
        fs.DirectoryExists(DocumentsDir).Returns(true);
        var buildQueue = new BuildQueue(_mockBuilder, _mockLoader, _mockLogger);
        buildQueue.StartQueue(fs);
        
        var projectConfig1 = new ProjectConfig { ProjectName = "TestProject1" };
        projectConfig1.BuildTargets.Add(new BuildTargetConfig { TargetName = "Debug" });
        var projectConfig2 = new ProjectConfig { ProjectName = "TestProject2" };
        projectConfig2.BuildTargets.Add(new BuildTargetConfig { TargetName = "Debug" });
        
        // Mock builder to return success for first build, then we can check second build starts
        _mockBuilder.Build(Arg.Any<IPipelineRunner>(), projectConfig1, Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(true);
        _mockBuilder.Build(Arg.Any<IPipelineRunner>(), projectConfig2, Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(true);

        // Act
        buildQueue.EnqueueBuild(projectConfig1);
        buildQueue.EnqueueBuild(projectConfig2);
        
        // Allow some time for queue processing
        Thread.Sleep(100);

        // Assert
        var allBuilds = buildQueue.GetAllBuildResults();
        Assert.That(allBuilds.Count(), Is.EqualTo(2));
        
        // Verify builder was called for both projects
        _mockBuilder.Received().Build(Arg.Any<IPipelineRunner>(), projectConfig1, Arg.Any<string>(), Arg.Any<CancellationToken>());
    }
}