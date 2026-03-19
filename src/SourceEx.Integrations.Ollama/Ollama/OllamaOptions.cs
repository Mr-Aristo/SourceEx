using System.ComponentModel.DataAnnotations;

namespace SourceEx.Integrations.Ollama.Ollama;

/// <summary>
/// Represents configuration for the Ollama integration.
/// </summary>
public sealed class OllamaOptions
{
    public const string SectionName = "Ollama";

    public bool Enabled { get; init; } = true;

    [Required]
    public string BaseUrl { get; init; } = "http://localhost:11434";

    [Required]
    public string Model { get; init; } = "gemma3";

    [Range(5, 300)]
    public int TimeoutSeconds { get; init; } = 60;
}
