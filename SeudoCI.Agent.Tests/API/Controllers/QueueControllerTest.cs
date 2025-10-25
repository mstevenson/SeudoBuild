namespace SeudoCI.Agent.Tests.API.Controllers;

using Microsoft.AspNetCore.Mvc;
using NSubstitute;
using SeudoCI.Agent.API.Controllers;
using SeudoCI.Agent;
using SeudoCI.Pipeline;
using System.Text.Json;

[TestFixture]
public class QueueControllerTest
{
    private IBuildQueue _mockBuildQueue;
    private QueueController _controller;

    [SetUp]
    public void SetUp()
    {
        _mockBuildQueue = Substitute.For<IBuildQueue>();
        _controller = new QueueController(_mockBuildQueue);
    }

    [Test]
    public void GetAllBuilds_EmptyQueue_ReturnsEmptyQueueStatus()
    {
        // Arrange
        _mockBuildQueue.GetAllBuildResults().Returns([]);

        // Act
        var result = _controller.GetAllBuilds();

        // Assert
        Assert.That(result, Is.InstanceOf<OkObjectResult>());
        var okResult = (OkObjectResult)result;
        Assert.That(okResult.Value, Is.Not.Null);
        
        // Serialize and deserialize to access anonymous object properties
        var json = JsonSerializer.Serialize(okResult.Value);
        using var document = JsonDocument.Parse(json);
        var root = document.RootElement;
        
        Assert.That(root.GetProperty("TotalBuilds").GetInt32(), Is.EqualTo(0));
        Assert.That(root.GetProperty("QueuedBuilds").GetInt32(), Is.EqualTo(0));
        Assert.That(root.GetProperty("RunningBuilds").GetInt32(), Is.EqualTo(0));
        Assert.That(root.GetProperty("CompletedBuilds").GetInt32(), Is.EqualTo(0));
        Assert.That(root.GetProperty("Builds").GetArrayLength(), Is.EqualTo(0));
    }

    [Test]
    public void GetAllBuilds_MultipleBuildsInDifferentStates_ReturnsCorrectCounts()
    {
        // Arrange
        var projectConfig = new ProjectConfig { ProjectName = "TestProject" };
        var builds = new[]
        {
            new BuildResult
            {
                Id = 1,
                ProjectConfiguration = projectConfig,
                TargetName = "Debug",
                BuildStatus = BuildResult.Status.Queued
            },
            new BuildResult
            {
                Id = 2,
                ProjectConfiguration = projectConfig,
                TargetName = "Release",
                BuildStatus = BuildResult.Status.Complete
            },
            new BuildResult
            {
                Id = 3,
                ProjectConfiguration = projectConfig,
                TargetName = "Test",
                BuildStatus = BuildResult.Status.Cancelled
            },
            new BuildResult
            {
                Id = 4,
                ProjectConfiguration = projectConfig,
                TargetName = "Deploy",
                BuildStatus = BuildResult.Status.Failed
            },
            new BuildResult
            {
                Id = 5,
                ProjectConfiguration = projectConfig,
                TargetName = "Hotfix",
                BuildStatus = BuildResult.Status.Running
            }
        };
        _mockBuildQueue.GetAllBuildResults().Returns(builds);

        // Act
        var result = _controller.GetAllBuilds();

        // Assert
        Assert.That(result, Is.InstanceOf<OkObjectResult>());
        var okResult = (OkObjectResult)result;
        Assert.That(okResult.Value, Is.Not.Null);
        
        // Serialize and deserialize to access anonymous object properties
        var json = JsonSerializer.Serialize(okResult.Value);
        using var document = JsonDocument.Parse(json);
        var root = document.RootElement;
        
        Assert.That(root.GetProperty("TotalBuilds").GetInt32(), Is.EqualTo(5));
        Assert.That(root.GetProperty("QueuedBuilds").GetInt32(), Is.EqualTo(1)); // Only the Queued status
        Assert.That(root.GetProperty("RunningBuilds").GetInt32(), Is.EqualTo(1));
        Assert.That(root.GetProperty("CompletedBuilds").GetInt32(), Is.EqualTo(2)); // Complete + Cancelled
        Assert.That(root.GetProperty("Builds").GetArrayLength(), Is.EqualTo(5));
        
        // Verify build results (access through the original response)
        var originalResponse = okResult.Value!;
        var buildsProperty = originalResponse.GetType().GetProperty("Builds");
        var buildsList = (IEnumerable<BuildResult>)buildsProperty!.GetValue(originalResponse)!;
        var firstBuild = buildsList.First(b => b.Id == 1);
        Assert.That(firstBuild.ProjectConfiguration.ProjectName, Is.EqualTo("TestProject"));
        Assert.That(firstBuild.TargetName, Is.EqualTo("Debug"));
        Assert.That(firstBuild.BuildStatus, Is.EqualTo(BuildResult.Status.Queued));
    }

    [Test]
    public void GetAllBuilds_ExceptionThrown_ReturnsBadRequest()
    {
        // Arrange
        _mockBuildQueue.When(x => x.GetAllBuildResults())
                      .Do(x => throw new InvalidOperationException("Database error"));

        // Act
        var result = _controller.GetAllBuilds();

        // Assert
        Assert.That(result, Is.InstanceOf<BadRequestResult>());
    }

    [Test]
    public void GetBuild_ExistingBuildId_ReturnsBuildResult()
    {
        // Arrange
        var buildId = 123;
        var projectConfig = new ProjectConfig { ProjectName = "TestProject" };
        var buildResult = new BuildResult 
        { 
            Id = buildId, 
            ProjectConfiguration = projectConfig,
            TargetName = "Release",
            BuildStatus = BuildResult.Status.Complete 
        };
        _mockBuildQueue.GetBuildResult(buildId).Returns(buildResult);

        // Act
        var result = _controller.GetBuild(buildId);

        // Assert
        Assert.That(result, Is.InstanceOf<OkObjectResult>());
        var okResult = (OkObjectResult)result;
        Assert.That(okResult.Value, Is.InstanceOf<BuildResult>());
        
        var retrievedResult = (BuildResult)okResult.Value!;
        Assert.That(retrievedResult.Id, Is.EqualTo(buildId));
        Assert.That(retrievedResult.ProjectConfiguration.ProjectName, Is.EqualTo("TestProject"));
        Assert.That(retrievedResult.TargetName, Is.EqualTo("Release"));
        Assert.That(retrievedResult.BuildStatus, Is.EqualTo(BuildResult.Status.Complete));
    }

    [Test]
    public void GetBuild_NonExistentBuildId_ReturnsNotFound()
    {
        // Arrange
        var buildId = 999;
        _mockBuildQueue.GetBuildResult(buildId).Returns((BuildResult)null);

        // Act
        var result = _controller.GetBuild(buildId);

        // Assert
        Assert.That(result, Is.InstanceOf<NotFoundResult>());
    }

    [Test]
    public void GetBuild_ExceptionThrown_ReturnsBadRequest()
    {
        // Arrange
        var buildId = 123;
        _mockBuildQueue.When(x => x.GetBuildResult(buildId))
                      .Do(x => throw new InvalidOperationException("Database error"));

        // Act
        var result = _controller.GetBuild(buildId);

        // Assert
        Assert.That(result, Is.InstanceOf<BadRequestResult>());
    }

    [Test]
    public void CancelBuild_ValidBuildId_ReturnsOk()
    {
        // Arrange
        var buildId = 123;

        // Act
        var result = _controller.CancelBuild(buildId);

        // Assert
        Assert.That(result, Is.InstanceOf<OkResult>());
        _mockBuildQueue.Received(1).CancelBuild(buildId);
    }

    [Test]
    public void CancelBuild_ExceptionThrown_ReturnsBadRequest()
    {
        // Arrange
        var buildId = 123;
        _mockBuildQueue.When(x => x.CancelBuild(buildId))
                      .Do(x => throw new InvalidOperationException("Cannot cancel completed build"));

        // Act
        var result = _controller.CancelBuild(buildId);

        // Assert
        Assert.That(result, Is.InstanceOf<BadRequestResult>());
    }

    [Test]
    public void CancelBuild_NonExistentBuildId_StillReturnsOk()
    {
        // Arrange
        var buildId = 999;
        _mockBuildQueue.CancelBuild(buildId).Returns((BuildResult)null);

        // Act
        var result = _controller.CancelBuild(buildId);

        // Assert
        // The controller doesn't check the return value, so it still returns Ok
        // This matches the original Nancy implementation behavior
        Assert.That(result, Is.InstanceOf<OkResult>());
        _mockBuildQueue.Received(1).CancelBuild(buildId);
    }
}