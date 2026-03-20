using BuildingBlocks.Messaging;
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

builder.Services.AddMessageBroker(builder.Configuration, typeof(ExpenseCreatedIntegrationEventConsumer).Assembly);
builder.Services.AddOllamaIntegration(builder.Configuration);

var host = builder.Build();
host.Run();
