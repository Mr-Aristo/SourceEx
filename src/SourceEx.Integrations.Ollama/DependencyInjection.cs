using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Refit;
using SourceEx.Integrations.Ollama.Ollama;

namespace SourceEx.Integrations.Ollama;

/// <summary>
/// Registers Ollama integration services.
/// </summary>
public static class DependencyInjection
{
    public static IServiceCollection AddOllamaIntegration(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddOptions<OllamaOptions>()
            .Bind(configuration.GetSection(OllamaOptions.SectionName))
            .ValidateDataAnnotations()
            .ValidateOnStart();

        services.AddRefitClient<IOllamaApi>()
            .ConfigureHttpClient((serviceProvider, client) =>
            {
                var options = serviceProvider.GetRequiredService<Microsoft.Extensions.Options.IOptions<OllamaOptions>>().Value;
                client.BaseAddress = new Uri(options.BaseUrl);
                client.Timeout = TimeSpan.FromSeconds(options.TimeoutSeconds);
            })
            .AddStandardResilienceHandler();

        services.AddScoped<IExpenseRiskAssessmentService, OllamaExpenseRiskAssessmentService>();

        return services;
    }
}
