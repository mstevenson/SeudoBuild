namespace SeudoCI.Agent.API.Controllers;

using Microsoft.AspNetCore.Mvc;
using Agent;

/// <summary>
/// Controller for managing the build queue.
/// </summary>
[ApiController]
[Route("[controller]")]
[Route("api/[controller]")]
public class QueueController(IBuildQueue buildQueue) : ControllerBase
{
    /// <summary>
    /// Get information about all builds in the queue.
    /// </summary>
    /// <returns>List of all build results.</returns>
    [HttpGet]
    public IActionResult GetAllBuilds()
    {
        try
        {
            var results = buildQueue.GetAllBuildResults().ToArray();
            var response = new
            {
                Builds = results,
                TotalBuilds = results.Length,
                QueuedBuilds = results.Count(r => r.BuildStatus == BuildResult.Status.Queued),
                RunningBuilds = 0, // TODO: Add Running status to BuildResult.Status enum
                CompletedBuilds = results.Count(r => r.BuildStatus == BuildResult.Status.Complete || r.BuildStatus == BuildResult.Status.Cancelled)
            };
            return Ok(response);
        }
        catch
        {
            return BadRequest();
        }
    }

    /// <summary>
    /// Get information about a specific build task.
    /// </summary>
    /// <param name="id">The build ID.</param>
    /// <returns>Build result information.</returns>
    [HttpGet("{id:int}")]
    public IActionResult GetBuild(int id)
    {
        try
        {
            var result = buildQueue.GetBuildResult(id);
            if (result == null)
            {
                return NotFound();
            }
            return Ok(result);
        }
        catch
        {
            return BadRequest();
        }
    }

    /// <summary>
    /// Cancel a specific build task.
    /// </summary>
    /// <param name="id">The build ID to cancel.</param>
    /// <returns>Success status.</returns>
    [HttpPost("{id:int}/cancel")]
    public IActionResult CancelBuild(int id)
    {
        try
        {
            buildQueue.CancelBuild(id);
            return Ok();
        }
        catch
        {
            return BadRequest();
        }
    }
}