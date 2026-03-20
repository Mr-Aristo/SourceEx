using BuildingBlocks.Messaging;
using BuildingBlocks.Observability;
using SourceEx.Integrations.Ollama;
using SourceEx.Worker.Policy.Consumers;

var builder = Host.CreateApplicationBuilder(args);

builder.Logging.Configure(options =>
{
    options.ActivityTrackingOptions =
        ActivityTrackingOptions.TraceId |
        ActivityTrackingOptions.SpanId |
        ActivityTrackingOptions.ParentId;
});

builder.AddSourceExStructuredLogging("sourceex-worker-policy");
builder.Services.AddMessageBroker(builder.Configuration, typeof(ExpenseCreatedIntegrationEventConsumer).Assembly);
builder.Services.AddOllamaIntegration(builder.Configuration);

var host = builder.Build();
host.Run();
