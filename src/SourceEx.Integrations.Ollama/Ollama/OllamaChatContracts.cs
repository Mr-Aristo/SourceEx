namespace SourceEx.Integrations.Ollama.Ollama;

/// <summary>
/// Represents a chat completion request sent to Ollama.
/// </summary>
public sealed record OllamaChatRequest(
    string Model,
    IReadOnlyCollection<OllamaChatMessage> Messages,
    object? Format,
    bool Stream,
    OllamaRequestOptions? Options = null);

/// <summary>
/// Represents a single chat message in the Ollama request.
/// </summary>
public sealed record OllamaChatMessage(string Role, string Content);

/// <summary>
/// Represents an Ollama chat completion response.
/// </summary>
public sealed record OllamaChatResponse(
    string Model,
    DateTime CreatedAt,
    OllamaResponseMessage Message,
    bool Done);

/// <summary>
/// Represents the generated assistant message returned by Ollama.
/// </summary>
public sealed record OllamaResponseMessage(string Role, string Content);

/// <summary>
/// Represents optional runtime generation settings for Ollama.
/// </summary>
public sealed record OllamaRequestOptions(
    decimal? Temperature = null,
    int? NumPredict = null);
