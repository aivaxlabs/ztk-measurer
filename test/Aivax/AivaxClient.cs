using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;

namespace CountTokens_Tester.Aivax;

internal sealed class AivaxClient
{
    private readonly HttpClient _http;
    private readonly Uri _endpoint;
    private readonly string _apiKey;

    public AivaxClient(HttpClient http, Uri endpoint, string apiKey)
    {
        _http = http ?? throw new ArgumentNullException(nameof(http));
        _endpoint = endpoint ?? throw new ArgumentNullException(nameof(endpoint));
        _apiKey = string.IsNullOrWhiteSpace(apiKey) ? throw new ArgumentException("API key is required", nameof(apiKey)) : apiKey;
    }

    public async Task<ChatCompletionResponse> CreateChatCompletionAsync(ChatCompletionRequest request, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);

        using var httpRequest = new HttpRequestMessage(HttpMethod.Post, _endpoint);
        httpRequest.Headers.Authorization = new AuthenticationHeaderValue("Bearer", _apiKey);
        httpRequest.Content = new StringContent(JsonSerializer.Serialize(request, JsonDefaults.SerializerOptions), Encoding.UTF8, "application/json");

        using HttpResponseMessage response = await _http.SendAsync(httpRequest, HttpCompletionOption.ResponseHeadersRead, cancellationToken);
        string body = await response.Content.ReadAsStringAsync(cancellationToken);

        if (!response.IsSuccessStatusCode)
            throw new AivaxApiException((int)response.StatusCode, body);

        ChatCompletionResponse? parsed = JsonSerializer.Deserialize<ChatCompletionResponse>(body, JsonDefaults.SerializerOptions);
        if (parsed is null)
            throw new AivaxApiException((int)response.StatusCode, "Failed to deserialize response.");

        return parsed;
    }
}
