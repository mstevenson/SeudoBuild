namespace SeudoCI.Agent.Tests;

using System.Net;
using NSubstitute;
using SeudoCI.Core;
using SeudoCI.Net;

[TestFixture]
public class BuildSubmitterTest
{
    private ILogger _mockLogger;
    private HttpClient _mockHttpClient;
    private AgentDiscoveryClient _mockDiscoveryClient;
    private BuildSubmitter _buildSubmitter;

    [SetUp]
    public void SetUp()
    {
        _mockLogger = Substitute.For<ILogger>();
        _mockHttpClient = new HttpClient();
        _mockDiscoveryClient = Substitute.For<AgentDiscoveryClient>();
        _buildSubmitter = new BuildSubmitter(_mockLogger, _mockHttpClient);
    }

    [TearDown]
    public void TearDown()
    {
        _mockHttpClient?.Dispose();
        _mockDiscoveryClient?.Dispose();
    }

    [Test]
    public async Task SubmitAsync_StartsDiscoveryClient()
    {
        // Arrange
        var projectYaml = "projectName: TestProject";
        var target = "Debug";
        var agentName = "test-agent";

        // Act
        var result = await _buildSubmitter.SubmitAsync(_mockDiscoveryClient, projectYaml, target, agentName);

        // Assert
        _mockDiscoveryClient.Received(1).Start();
        _mockLogger.Received(1).Write("Submitting build to test-agent");
    }

    [Test]
    public async Task SubmitAsync_DiscoveryStartThrowsSocketException_ReturnsFalse()
    {
        // Arrange
        var projectYaml = "projectName: TestProject";
        var target = "Debug";
        var agentName = "test-agent";

        _mockDiscoveryClient.When(x => x.Start())
            .Do(x => throw new System.Net.Sockets.SocketException(10048)); // Address already in use

        // Act
        var result = await _buildSubmitter.SubmitAsync(_mockDiscoveryClient, projectYaml, target, agentName);

        // Assert
        Assert.That(result, Is.False);
        _mockLogger.Received(1).Write(Arg.Is<string>(s => s.Contains("Could not start build agent discovery client")), LogType.Failure);
    }

    [Test]
    public async Task SubmitAsync_CancellationRequested_ReturnsFalse()
    {
        // Arrange
        var projectYaml = "projectName: TestProject";
        var target = "Debug";
        var agentName = "test-agent";
        var cts = new CancellationTokenSource();
        cts.Cancel();

        // Act
        var result = await _buildSubmitter.SubmitAsync(_mockDiscoveryClient, projectYaml, target, agentName, cts.Token);

        // Assert
        Assert.That(result, Is.False);
        _mockLogger.Received(1).Write(Arg.Is<string>(s => s.Contains("Discovery cancelled while looking for agent 'test-agent'")), LogType.Failure);
        _mockDiscoveryClient.Received(1).Stop();
    }

    [Test]
    public async Task SubmitAsync_NormalExecution_StopsDiscoveryClient()
    {
        // Arrange
        var projectYaml = "projectName: TestProject";
        var target = "Debug";
        var agentName = "test-agent";

        // Act
        await _buildSubmitter.SubmitAsync(_mockDiscoveryClient, projectYaml, target, agentName);

        // Assert
        _mockDiscoveryClient.Received(1).Stop();
    }

    [Test]
    public async Task SubmitAsync_LogsExpectedMessages()
    {
        // Arrange
        var projectYaml = "projectName: TestProject";
        var target = "Debug";
        var agentName = "test-agent";

        // Act
        await _buildSubmitter.SubmitAsync(_mockDiscoveryClient, projectYaml, target, agentName);

        // Assert
        _mockLogger.Received(1).Write("Submitting build to test-agent");
        _mockLogger.Received(1).Write("Starting agent discovery (current implementation is limited)", LogType.Alert);
        _mockLogger.Received(1).Write("Agent discovery client implementation needs completion for full functionality", LogType.Alert);
        _mockLogger.Received(1).Write("Build submission via discovery not yet supported", LogType.Failure);
    }

    [Test]
    public async Task SubmitAsync_WithTimeout_CompletesWithinReasonableTime()
    {
        // Arrange
        var projectYaml = "projectName: TestProject";
        var target = "Debug";
        var agentName = "test-agent";
        var cts = new CancellationTokenSource(TimeSpan.FromSeconds(1));

        // Act
        var stopwatch = System.Diagnostics.Stopwatch.StartNew();
        var result = await _buildSubmitter.SubmitAsync(_mockDiscoveryClient, projectYaml, target, agentName, cts.Token);
        stopwatch.Stop();

        // Assert
        Assert.That(result, Is.False);
        Assert.That(stopwatch.ElapsedMilliseconds, Is.LessThan(2000)); // Should complete before 2 seconds due to cancellation
        _mockLogger.Received(1).Write(Arg.Is<string>(s => s.Contains("Discovery cancelled")), LogType.Failure);
    }

    [Test]
    public async Task SubmitAsync_WithEmptyAgentName_StillProcesses()
    {
        // Arrange
        var projectYaml = "projectName: TestProject";
        var target = "Debug";
        var agentName = "";

        // Act
        var result = await _buildSubmitter.SubmitAsync(_mockDiscoveryClient, projectYaml, target, agentName);

        // Assert
        Assert.That(result, Is.False);
        _mockLogger.Received(1).Write("Submitting build to ");
        _mockDiscoveryClient.Received(1).Start();
        _mockDiscoveryClient.Received(1).Stop();
    }

    [Test]
    public async Task SubmitAsync_WithNullTarget_StillProcesses()
    {
        // Arrange
        var projectYaml = "projectName: TestProject";
        string target = null;
        var agentName = "test-agent";

        // Act
        var result = await _buildSubmitter.SubmitAsync(_mockDiscoveryClient, projectYaml, target, agentName);

        // Assert
        Assert.That(result, Is.False);
        _mockLogger.Received(1).Write("Submitting build to test-agent");
        _mockDiscoveryClient.Received(1).Start();
        _mockDiscoveryClient.Received(1).Stop();
    }

    [Test]
    public async Task SubmitAsync_WithSpecialCharactersInAgentName_HandlesCorrectly()
    {
        // Arrange
        var projectYaml = "projectName: TestProject";
        var target = "Debug";
        var agentName = "test-agent-123_special.name";

        // Act
        var result = await _buildSubmitter.SubmitAsync(_mockDiscoveryClient, projectYaml, target, agentName);

        // Assert
        Assert.That(result, Is.False);
        _mockLogger.Received(1).Write("Submitting build to test-agent-123_special.name");
    }

    [Test]
    public async Task SubmitAsync_MultipleCalls_EachCallStartsAndStopsDiscovery()
    {
        // Arrange
        var projectYaml = "projectName: TestProject";
        var target = "Debug";
        var agentName = "test-agent";

        // Act
        await _buildSubmitter.SubmitAsync(_mockDiscoveryClient, projectYaml, target, agentName);
        await _buildSubmitter.SubmitAsync(_mockDiscoveryClient, projectYaml, target, agentName);

        // Assert
        _mockDiscoveryClient.Received(2).Start();
        _mockDiscoveryClient.Received(2).Stop();
        _mockLogger.Received(2).Write("Submitting build to test-agent");
    }

    [Test]
    public async Task SubmitAsync_DiscoveryThrowsException_StillStopsDiscovery()
    {
        // Arrange
        var projectYaml = "projectName: TestProject";
        var target = "Debug";
        var agentName = "test-agent";

        _mockDiscoveryClient.When(x => x.Start())
            .Do(x => throw new InvalidOperationException("Discovery error"));

        // Act
        var result = await _buildSubmitter.SubmitAsync(_mockDiscoveryClient, projectYaml, target, agentName);

        // Assert
        Assert.That(result, Is.False);
        _mockDiscoveryClient.Received(1).Stop(); // Should still call Stop in finally block
    }
}