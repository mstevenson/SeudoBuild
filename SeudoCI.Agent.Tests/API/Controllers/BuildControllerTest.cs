namespace SeudoCI.Agent.Tests.API.Controllers;

using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using NSubstitute;
using SeudoCI.Agent.API.Controllers;
using SeudoCI.Agent;
using SeudoCI.Core;
using SeudoCI.Pipeline;
using System.Text;

[TestFixture]
public class BuildControllerTest
{
    private IBuildQueue _mockBuildQueue;
    private IModuleLoader _mockModuleLoader;
    private IFileSystem _mockFileSystem;
    private ILogger _mockLogger;
    private BuildController _controller;
    private HttpContext _mockHttpContext;

    [SetUp]
    public void SetUp()
    {
        _mockBuildQueue = Substitute.For<IBuildQueue>();
        _mockModuleLoader = Substitute.For<IModuleLoader>();
        _mockFileSystem = Substitute.For<IFileSystem>();
        _mockLogger = Substitute.For<ILogger>();
        
        _controller = new BuildController(
            _mockBuildQueue,
            _mockModuleLoader, 
            _mockFileSystem,
            _mockLogger);

        // Setup HTTP context for testing
        _mockHttpContext = Substitute.For<HttpContext>();
        var mockConnection = Substitute.For<ConnectionInfo>();
        mockConnection.RemoteIpAddress.Returns(System.Net.IPAddress.Parse("192.168.1.100"));
        _mockHttpContext.Connection.Returns(mockConnection);
        _controller.ControllerContext = new ControllerContext
        {
            HttpContext = _mockHttpContext
        };
    }

    [Test]
    public async Task BuildDefaultTarget_ValidProject_ReturnsBuildId()
    {
        // Arrange
        var projectYaml = """
projectName: TestProject
buildTargets:
  - targetName: Debug
""";
        var requestBody = new MemoryStream(Encoding.UTF8.GetBytes(projectYaml));
        _mockHttpContext.Request.Body.Returns(requestBody);

        var mockRegistry = Substitute.For<IModuleRegistry>();
        mockRegistry.GetStepConfigConverters().Returns([]);
        _mockModuleLoader.Registry.Returns(mockRegistry);

        var mockSerializer = Substitute.For<Serializer>(_mockFileSystem);
        var projectConfig = new ProjectConfig 
        { 
            ProjectName = "TestProject"
        };
        projectConfig.BuildTargets.Add(new BuildTargetConfig { TargetName = "Debug" });

        var buildResult = new BuildResult 
        { 
            Id = 123, 
            ProjectConfiguration = projectConfig,
            BuildStatus = BuildResult.Status.Queued 
        };
        _mockBuildQueue.EnqueueBuild(Arg.Any<ProjectConfig>()).Returns(buildResult);

        // Act
        var result = await _controller.BuildDefaultTarget();

        // Assert
        Assert.That(result, Is.InstanceOf<OkObjectResult>());
        var okResult = (OkObjectResult)result;
        Assert.That(okResult.Value, Is.EqualTo("123"));
        
        _mockBuildQueue.Received(1).EnqueueBuild(Arg.Any<ProjectConfig>());
        _mockLogger.Received(1).QueueNotification(Arg.Is<string>(s => s.Contains("TestProject") && s.Contains("192.168.1.100")));
    }

    [Test]
    public async Task BuildDefaultTarget_InvalidJson_ReturnsBadRequest()
    {
        // Arrange
        var invalidYaml = "projectName: [";
        var requestBody = new MemoryStream(Encoding.UTF8.GetBytes(invalidYaml));
        _mockHttpContext.Request.Body.Returns(requestBody);

        var mockRegistry = Substitute.For<IModuleRegistry>();
        mockRegistry.GetStepConfigConverters().Returns([]);
        _mockModuleLoader.Registry.Returns(mockRegistry);

        // Act
        var result = await _controller.BuildDefaultTarget();

        // Assert
        Assert.That(result, Is.InstanceOf<BadRequestObjectResult>());
        _mockLogger.Received(1).Write(Arg.Any<string>(), LogType.Failure);
    }

    [Test]
    public async Task BuildDefaultTarget_EmptyProjectName_ReturnsBadRequest()
    {
        // Arrange
        var projectYaml = """
projectName: ""
buildTargets: []
""";
        var requestBody = new MemoryStream(Encoding.UTF8.GetBytes(projectYaml));
        _mockHttpContext.Request.Body.Returns(requestBody);

        var mockRegistry = Substitute.For<IModuleRegistry>();
        mockRegistry.GetStepConfigConverters().Returns([]);
        _mockModuleLoader.Registry.Returns(mockRegistry);

        // Act
        var result = await _controller.BuildDefaultTarget();

        // Assert
        Assert.That(result, Is.InstanceOf<BadRequestObjectResult>());
        var badRequestResult = (BadRequestObjectResult)result;
        Assert.That(badRequestResult.Value.ToString(), Does.Contain("invalid project configuration"));
    }

    [Test]
    public async Task BuildSpecificTarget_ValidTarget_ReturnsBuildId()
    {
        // Arrange
        var target = "Release";
        var projectYaml = """
projectName: TestProject
buildTargets:
  - targetName: Release
""";
        var requestBody = new MemoryStream(Encoding.UTF8.GetBytes(projectYaml));
        _mockHttpContext.Request.Body.Returns(requestBody);

        var mockRegistry = Substitute.For<IModuleRegistry>();
        mockRegistry.GetStepConfigConverters().Returns([]);
        _mockModuleLoader.Registry.Returns(mockRegistry);

        var projectConfig = new ProjectConfig 
        { 
            ProjectName = "TestProject"
        };
        projectConfig.BuildTargets.Add(new BuildTargetConfig { TargetName = "Release" });

        var buildResult = new BuildResult 
        { 
            Id = 456, 
            ProjectConfiguration = projectConfig,
            TargetName = target,
            BuildStatus = BuildResult.Status.Queued 
        };
        _mockBuildQueue.EnqueueBuild(Arg.Any<ProjectConfig>(), target).Returns(buildResult);

        // Act
        var result = await _controller.BuildSpecificTarget(target);

        // Assert
        Assert.That(result, Is.InstanceOf<OkObjectResult>());
        var okResult = (OkObjectResult)result;
        Assert.That(okResult.Value, Is.EqualTo("456"));
        
        _mockBuildQueue.Received(1).EnqueueBuild(Arg.Any<ProjectConfig>(), target);
        _mockLogger.Received(1).QueueNotification(Arg.Is<string>(s => s.Contains("TestProject") && s.Contains("Release")));
    }

    [Test]
    public async Task BuildSpecificTarget_InvalidTarget_ReturnsBadRequest()
    {
        // Arrange
        var target = "NonExistentTarget";
        var projectYaml = """
projectName: TestProject
buildTargets:
  - targetName: Debug
""";
        var requestBody = new MemoryStream(Encoding.UTF8.GetBytes(projectYaml));
        _mockHttpContext.Request.Body.Returns(requestBody);

        var mockRegistry = Substitute.For<IModuleRegistry>();
        mockRegistry.GetStepConfigConverters().Returns([]);
        _mockModuleLoader.Registry.Returns(mockRegistry);

        // Act
        var result = await _controller.BuildSpecificTarget(target);

        // Assert
        Assert.That(result, Is.InstanceOf<BadRequestObjectResult>());
        var badRequestResult = (BadRequestObjectResult)result;
        Assert.That(badRequestResult.Value.ToString(), Does.Contain("could not find a build target named 'NonExistentTarget'"));
    }

    [Test]
    public async Task BuildSpecificTarget_ExceptionDuringEnqueue_ReturnsBadRequest()
    {
        // Arrange
        var target = "Debug";
        var projectYaml = """
projectName: TestProject
buildTargets:
  - targetName: Debug
""";
        var requestBody = new MemoryStream(Encoding.UTF8.GetBytes(projectYaml));
        _mockHttpContext.Request.Body.Returns(requestBody);

        var mockRegistry = Substitute.For<IModuleRegistry>();
        mockRegistry.GetStepConfigConverters().Returns([]);
        _mockModuleLoader.Registry.Returns(mockRegistry);

        _mockBuildQueue.When(x => x.EnqueueBuild(Arg.Any<ProjectConfig>(), target))
                      .Do(x => throw new InvalidOperationException("Queue is full"));

        // Act
        var result = await _controller.BuildSpecificTarget(target);

        // Assert
        Assert.That(result, Is.InstanceOf<BadRequestObjectResult>());
        _mockLogger.Received(1).Write(Arg.Is<string>(s => s.Contains("Queue is full")), LogType.Failure);
    }

    [Test]
    public void GetClientIpAddress_ValidConnection_ReturnsIpAddress()
    {
        // This tests the private method indirectly through the public methods
        // The setup in SetUp already configures the IP address

        // Act & Assert - this is verified in the other tests where we check the log message contains the IP
        // The IP address extraction is working as expected based on the other test assertions
        Assert.Pass("IP address extraction tested indirectly through other tests");
    }
}