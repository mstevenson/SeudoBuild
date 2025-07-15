namespace SeudoCI.Agent.API.Controllers;

using Microsoft.AspNetCore.Mvc;

/// <summary>
/// Controller for health checks and system status.
/// </summary>
[ApiController]
[Route("[controller]")]
[Route("api/[controller]")]
public class HealthController : ControllerBase
{
    /// <summary>
    /// Basic health check endpoint.
    /// </summary>
    /// <returns>Health status.</returns>
    [HttpGet]
    public IActionResult GetHealth()
    {
        return Ok(new { status = "healthy", timestamp = DateTime.UtcNow });
    }

    /// <summary>
    /// Detailed health check with system information.
    /// </summary>
    /// <returns>Detailed health status.</returns>
    [HttpGet("detailed")]
    public IActionResult GetDetailedHealth()
    {
        var health = new
        {
            status = "healthy",
            timestamp = DateTime.UtcNow,
            system = new
            {
                platform = Environment.OSVersion.Platform.ToString(),
                version = Environment.OSVersion.VersionString,
                dotnetVersion = Environment.Version.ToString(),
                machineName = Environment.MachineName,
                processorCount = Environment.ProcessorCount,
                workingSet = Environment.WorkingSet,
                uptime = TimeSpan.FromMilliseconds(Environment.TickCount64)
            }
        };

        return Ok(health);
    }
}