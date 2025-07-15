namespace SeudoCI.Agent.Tests.API.Controllers;

using Microsoft.AspNetCore.Mvc;
using SeudoCI.Agent.API.Controllers;

[TestFixture]
public class HealthControllerTest
{
    private HealthController _controller;

    [SetUp]
    public void SetUp()
    {
        _controller = new HealthController();
    }

    [Test]
    public void GetHealth_ReturnsHealthyStatus()
    {
        // Act
        var result = _controller.GetHealth();

        // Assert
        Assert.That(result, Is.InstanceOf<OkObjectResult>());
        var okResult = (OkObjectResult)result;
        
        // Verify the response contains the expected structure
        var responseValue = okResult.Value;
        Assert.That(responseValue, Is.Not.Null);
        
        // Use reflection to check the anonymous object properties
        var statusProperty = responseValue.GetType().GetProperty("status");
        var timestampProperty = responseValue.GetType().GetProperty("timestamp");
        
        Assert.That(statusProperty, Is.Not.Null);
        Assert.That(timestampProperty, Is.Not.Null);
        
        Assert.That(statusProperty.GetValue(responseValue), Is.EqualTo("healthy"));
        Assert.That(timestampProperty.GetValue(responseValue), Is.InstanceOf<DateTime>());
        
        var timestamp = (DateTime)timestampProperty.GetValue(responseValue);
        Assert.That(timestamp, Is.EqualTo(DateTime.UtcNow).Within(TimeSpan.FromSeconds(5)));
    }

    [Test]
    public void GetDetailedHealth_ReturnsDetailedHealthStatus()
    {
        // Act
        var result = _controller.GetDetailedHealth();

        // Assert
        Assert.That(result, Is.InstanceOf<OkObjectResult>());
        var okResult = (OkObjectResult)result;
        
        var responseValue = okResult.Value;
        Assert.That(responseValue, Is.Not.Null);
        
        // Check main properties
        var statusProperty = responseValue.GetType().GetProperty("status");
        var timestampProperty = responseValue.GetType().GetProperty("timestamp");
        var systemProperty = responseValue.GetType().GetProperty("system");
        
        Assert.That(statusProperty, Is.Not.Null);
        Assert.That(timestampProperty, Is.Not.Null);
        Assert.That(systemProperty, Is.Not.Null);
        
        Assert.That(statusProperty.GetValue(responseValue), Is.EqualTo("healthy"));
        Assert.That(timestampProperty.GetValue(responseValue), Is.InstanceOf<DateTime>());
        
        // Check system information
        var systemInfo = systemProperty.GetValue(responseValue);
        Assert.That(systemInfo, Is.Not.Null);
        
        var platformProperty = systemInfo.GetType().GetProperty("platform");
        var versionProperty = systemInfo.GetType().GetProperty("version");
        var dotnetVersionProperty = systemInfo.GetType().GetProperty("dotnetVersion");
        var machineNameProperty = systemInfo.GetType().GetProperty("machineName");
        var processorCountProperty = systemInfo.GetType().GetProperty("processorCount");
        var workingSetProperty = systemInfo.GetType().GetProperty("workingSet");
        var uptimeProperty = systemInfo.GetType().GetProperty("uptime");
        
        Assert.That(platformProperty, Is.Not.Null);
        Assert.That(versionProperty, Is.Not.Null);
        Assert.That(dotnetVersionProperty, Is.Not.Null);
        Assert.That(machineNameProperty, Is.Not.Null);
        Assert.That(processorCountProperty, Is.Not.Null);
        Assert.That(workingSetProperty, Is.Not.Null);
        Assert.That(uptimeProperty, Is.Not.Null);
        
        // Verify system information values are reasonable
        Assert.That(platformProperty.GetValue(systemInfo), Is.InstanceOf<string>());
        Assert.That(versionProperty.GetValue(systemInfo), Is.InstanceOf<string>());
        Assert.That(dotnetVersionProperty.GetValue(systemInfo), Is.InstanceOf<string>());
        Assert.That(machineNameProperty.GetValue(systemInfo), Is.InstanceOf<string>());
        Assert.That(processorCountProperty.GetValue(systemInfo), Is.InstanceOf<int>());
        Assert.That(workingSetProperty.GetValue(systemInfo), Is.InstanceOf<long>());
        Assert.That(uptimeProperty.GetValue(systemInfo), Is.InstanceOf<TimeSpan>());
        
        // Verify reasonable values
        var processorCount = (int)processorCountProperty.GetValue(systemInfo);
        var workingSet = (long)workingSetProperty.GetValue(systemInfo);
        var uptime = (TimeSpan)uptimeProperty.GetValue(systemInfo);
        
        Assert.That(processorCount, Is.GreaterThan(0));
        Assert.That(workingSet, Is.GreaterThan(0));
        Assert.That(uptime.TotalMilliseconds, Is.GreaterThanOrEqualTo(0));
    }

    [Test]
    public void GetHealth_CalledMultipleTimes_ReturnsConsistentFormat()
    {
        // Act
        var result1 = _controller.GetHealth();
        var result2 = _controller.GetHealth();

        // Assert
        Assert.That(result1, Is.InstanceOf<OkObjectResult>());
        Assert.That(result2, Is.InstanceOf<OkObjectResult>());
        
        var okResult1 = (OkObjectResult)result1;
        var okResult2 = (OkObjectResult)result2;
        
        // Both should have the same structure
        var response1 = okResult1.Value;
        var response2 = okResult2.Value;
        
        Assert.That(response1.GetType(), Is.EqualTo(response2.GetType()));
        
        var status1 = response1.GetType().GetProperty("status").GetValue(response1);
        var status2 = response2.GetType().GetProperty("status").GetValue(response2);
        
        Assert.That(status1, Is.EqualTo(status2));
        Assert.That(status1, Is.EqualTo("healthy"));
    }

    [Test]
    public void GetDetailedHealth_CalledMultipleTimes_HasUpdatedTimestamps()
    {
        // Act
        var result1 = _controller.GetDetailedHealth();
        Thread.Sleep(10); // Small delay to ensure different timestamps
        var result2 = _controller.GetDetailedHealth();

        // Assert
        var okResult1 = (OkObjectResult)result1;
        var okResult2 = (OkObjectResult)result2;
        
        var timestamp1 = (DateTime)okResult1.Value.GetType().GetProperty("timestamp").GetValue(okResult1.Value);
        var timestamp2 = (DateTime)okResult2.Value.GetType().GetProperty("timestamp").GetValue(okResult2.Value);
        
        Assert.That(timestamp2, Is.GreaterThanOrEqualTo(timestamp1));
    }
}