namespace SeudoCI.Agent;

using System.Text.Json;
using Net;
using Core;
using Services;

/// <summary>
/// Submit a build process to an agent on the local network.
/// </summary>
public class BuildSubmitter(ILogger logger, IHttpService httpService)
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        WriteIndented = false
    };

    /// <summary>
    /// Submit the given project configuration and build target to a build
    /// agent on the network.
    /// </summary>
    public async Task<bool> SubmitAsync(AgentDiscoveryClient agentDiscoveryClient, string projectJson, string target, string agentName, CancellationToken cancellationToken = default)
    {
        logger.Write("Submitting build to " + agentName);

        try
        {
            agentDiscoveryClient.Start();
        }
        catch (System.Net.Sockets.SocketException ex)
        {
            logger.Write($"Could not start build agent discovery client: {ex.Message}", LogType.Failure);
            return false;
        }

        // Note: Current AgentDiscoveryClient implementation is incomplete
        // It only logs discovered services without exposing public events
        // This is a placeholder implementation until the discovery client is fully implemented
        logger.Write("Starting agent discovery (current implementation is limited)", LogType.Alert);
        
        try
        {
            // Start discovery to trigger console logging
            // The current implementation will log discoveries but we can't access them programmatically
            await Task.Delay(5000, cancellationToken); // Give discovery some time
            
            logger.Write("Agent discovery client implementation needs completion for full functionality", LogType.Alert);
            logger.Write("Build submission via discovery not yet supported", LogType.Failure);
            return false;
        }
        catch (OperationCanceledException)
        {
            logger.Write($"Discovery cancelled while looking for agent '{agentName}'", LogType.Failure);
            return false;
        }
        finally
        {
            agentDiscoveryClient.Stop();
        }
    }

    private async Task<Agent?> GetAgentInfoAsync(string agentAddress, CancellationToken cancellationToken)
    {
        try
        {
            var json = await httpService.GetStringAsync(agentAddress, cancellationToken);
            return JsonSerializer.Deserialize<Agent>(json, JsonOptions);
        }
        catch (HttpRequestException ex)
        {
            logger.Write($"Failed to get agent info from {agentAddress}: {ex.Message}", LogType.Failure);
            return null;
        }
        catch (JsonException ex)
        {
            logger.Write($"Failed to parse agent info from {agentAddress}: {ex.Message}", LogType.Failure);
            return null;
        }
    }

    private async Task<bool> SubmitBuildRequestAsync(string address, int port, string projectJson, string target, CancellationToken cancellationToken)
    {
        try
        {
            var endpoint = string.IsNullOrEmpty(target) ? "build" : $"build/{target}";
            var requestUri = $"http://{address}:{port}/{endpoint}";

            logger.Write($"Sending build request to {requestUri}");
            
            var response = await httpService.PostJsonAsync(requestUri, projectJson, cancellationToken);
            
            if (response.IsSuccessStatusCode)
            {
                var responseContent = await response.Content.ReadAsStringAsync(cancellationToken);
                logger.Write($"Build submission successful. Response: {responseContent}");
                return true;
            }
            else
            {
                var errorContent = await response.Content.ReadAsStringAsync(cancellationToken);
                logger.Write($"Build submission failed with status {response.StatusCode}: {errorContent}", LogType.Failure);
                return false;
            }
        }
        catch (HttpRequestException ex)
        {
            logger.Write($"HTTP error during build submission: {ex.Message}", LogType.Failure);
            return false;
        }
        catch (TaskCanceledException ex) when (ex.InnerException is TimeoutException)
        {
            logger.Write("Build submission timed out", LogType.Failure);
            return false;
        }
    }
}