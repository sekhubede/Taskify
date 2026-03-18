using System.Net.Http.Json;
using System.Text.Json.Serialization;
using Microsoft.Extensions.Options;

namespace Taskify.Api.AI;

public sealed class OllamaAiProvider : IAiProvider
{
    private readonly HttpClient _httpClient;
    private readonly TaskifyAiOptions _options;

    public OllamaAiProvider(HttpClient httpClient, IOptions<TaskifyAiOptions> options)
    {
        _httpClient = httpClient;
        _options = options.Value;
    }

    public async Task<string> GenerateJsonAsync(
        string systemPrompt,
        string userPrompt,
        CancellationToken cancellationToken)
    {
        if (!_options.Enabled)
        {
            throw new InvalidOperationException("AI features are disabled.");
        }

        var payload = new OllamaChatRequest(
            Model: _options.Model,
            Stream: false,
            Format: "json",
            Messages:
            [
                new OllamaMessage("system", systemPrompt),
                new OllamaMessage("user", userPrompt)
            ],
            Options: new OllamaOptions(
                Temperature: 0.2,
                TopP: 0.9,
                NumPredict: 700
            )
        );

        using var response = await _httpClient.PostAsJsonAsync(
            "api/chat",
            payload,
            cancellationToken
        );

        if (!response.IsSuccessStatusCode)
        {
            var error = await response.Content.ReadAsStringAsync(cancellationToken);
            throw new HttpRequestException(
                $"Ollama request failed ({(int)response.StatusCode}): {error}"
            );
        }

        var parsed = await response.Content.ReadFromJsonAsync<OllamaChatResponse>(
            cancellationToken: cancellationToken
        );

        var content = parsed?.Message?.Content?.Trim();
        if (string.IsNullOrWhiteSpace(content))
        {
            throw new InvalidOperationException("Ollama returned an empty response.");
        }

        return content;
    }

    private sealed record OllamaChatRequest(
        string Model,
        bool Stream,
        string Format,
        List<OllamaMessage> Messages,
        OllamaOptions Options
    );

    private sealed record OllamaMessage(string Role, string Content);

    private sealed record OllamaOptions(
        [property: JsonPropertyName("temperature")] double Temperature,
        [property: JsonPropertyName("top_p")] double TopP,
        [property: JsonPropertyName("num_predict")] int NumPredict
    );

    private sealed record OllamaChatResponse(
        [property: JsonPropertyName("message")] OllamaMessage? Message
    );
}
