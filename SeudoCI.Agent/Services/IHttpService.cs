namespace SeudoCI.Agent.Services;

/// <summary>
/// Abstraction for HTTP operations to enable testing and configuration.
/// </summary>
public interface IHttpService
{
    /// <summary>
    /// Sends a GET request to the specified URI and returns the response body as a string.
    /// </summary>
    Task<string> GetStringAsync(string requestUri, CancellationToken cancellationToken = default);

    /// <summary>
    /// Sends a POST request with JSON content to the specified URI.
    /// </summary>
    Task<HttpResponseMessage> PostJsonAsync(string requestUri, string jsonContent, CancellationToken cancellationToken = default);

    /// <summary>
    /// Sends a POST request with custom content to the specified URI.
    /// </summary>
    Task<HttpResponseMessage> PostAsync(string requestUri, HttpContent content, CancellationToken cancellationToken = default);
}