using Refit;

namespace SourceEx.Integrations.Ollama.Ollama;

/// <summary>
/// Refit contract for the Ollama HTTP API.
/// </summary>
public interface IOllamaApi
{
    [Post("/api/chat")]
    Task<OllamaChatResponse> ChatAsync([Body] OllamaChatRequest request, CancellationToken cancellationToken = default);
}
