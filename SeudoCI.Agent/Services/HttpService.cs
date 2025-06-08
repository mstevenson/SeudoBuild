namespace SeudoCI.Agent.Services;

using System.Text;

/// <summary>
/// Default HTTP service implementation using HttpClient.
/// </summary>
public class HttpService(HttpClient httpClient) : IHttpService
{
    private readonly HttpClient _httpClient = httpClient ?? throw new ArgumentNullException(nameof(httpClient));

    public async Task<string> GetStringAsync(string requestUri, CancellationToken cancellationToken = default)
    {
        var response = await _httpClient.GetAsync(requestUri, cancellationToken);
        response.EnsureSuccessStatusCode();
        return await response.Content.ReadAsStringAsync(cancellationToken);
    }

    public async Task<HttpResponseMessage> PostJsonAsync(string requestUri, string jsonContent, CancellationToken cancellationToken = default)
    {
        var content = new StringContent(jsonContent, Encoding.UTF8, "application/json");
        return await _httpClient.PostAsync(requestUri, content, cancellationToken);
    }

    public async Task<HttpResponseMessage> PostAsync(string requestUri, HttpContent content, CancellationToken cancellationToken = default)
    {
        return await _httpClient.PostAsync(requestUri, content, cancellationToken);
    }
}