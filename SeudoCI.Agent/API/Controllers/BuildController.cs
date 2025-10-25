namespace SeudoCI.Agent.API.Controllers;

using Microsoft.AspNetCore.Mvc;
using Agent;
using Core;
using Pipeline;
using System.Text;

/// <summary>
/// Controller for managing build operations.
/// </summary>
[ApiController]
[Route("[controller]")]
[Route("api/[controller]")]
public class BuildController(
    IBuildQueue buildQueue,
    IModuleLoader moduleLoader,
    IFileSystem filesystem,
    ILogger logger)
    : ControllerBase
{
    /// <summary>
    /// Queue a build for the default target in the project configuration.
    /// </summary>
    /// <returns>The build ID as a string.</returns>
    [HttpPost]
    public async Task<IActionResult> BuildDefaultTarget()
    {
        try
        {
            var config = await ProcessReceivedBuildRequestAsync(null);
            logger.QueueNotification($"Received build request: project '{config.ProjectName}', default target, from {GetClientIpAddress()}");
            var buildRequest = buildQueue.EnqueueBuild(config);
            return Ok(buildRequest.Id.ToString());
        }
        catch (Exception e)
        {
            logger.Write(e.Message, LogType.Failure);
            return BadRequest(e.Message);
        }
    }

    /// <summary>
    /// Queue a build for a specific target within the project configuration.
    /// </summary>
    /// <param name="target">Name of the target to build.</param>
    /// <returns>The build ID as a string.</returns>
    [HttpPost("{target}")]
    public async Task<IActionResult> BuildSpecificTarget(string target)
    {
        try
        {
            var config = await ProcessReceivedBuildRequestAsync(target);
            logger.QueueNotification($"Queuing build request: project '{config.ProjectName}', target '{target}', from {GetClientIpAddress()}");
            var buildRequest = buildQueue.EnqueueBuild(config, target);
            return Ok(buildRequest.Id.ToString());
        }
        catch (Exception e)
        {
            logger.Write(e.Message, LogType.Failure);
            return BadRequest(e.Message);
        }
    }

    private async Task<ProjectConfig> ProcessReceivedBuildRequestAsync(string? target)
    {
        // Read YAML from request body
        using var reader = new StreamReader(Request.Body, Encoding.UTF8);
        var yaml = await reader.ReadToEndAsync();

        // Deserialize with custom type discriminators
        var discriminators = moduleLoader.Registry.GetStepConfigConverters();
        var serializer = new Serializer(filesystem);
        var config = serializer.Deserialize<ProjectConfig>(yaml, discriminators);

        // Validate target exists if specified
        if (!string.IsNullOrEmpty(target))
        {
            if (!config.BuildTargets.Exists(t => t.TargetName == target))
            {
                throw new Exception($"‣ Received project configuration from {GetClientIpAddress()} but could not find a build target named '{target}'");
            }
        }

        // Validate project name
        if (string.IsNullOrEmpty(config.ProjectName))
        {
            throw new Exception($"‣ Received invalid project configuration from {GetClientIpAddress()}");
        }

        return config;
    }

    private string GetClientIpAddress()
    {
        return HttpContext.Connection.RemoteIpAddress?.ToString() ?? "unknown";
    }
}