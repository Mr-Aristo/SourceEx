using BuildingBlocks.Messaging;
using BuildingBlocks.Observability;
using SourceEx.Worker.Audit.Consumers;

var builder = Host.CreateApplicationBuilder(args);

builder.Logging.Configure(options =>
{
    options.ActivityTrackingOptions =
        ActivityTrackingOptions.TraceId |
        ActivityTrackingOptions.SpanId |
        ActivityTrackingOptions.ParentId;
});

builder.AddSourceExStructuredLogging("sourceex-worker-audit");
builder.Services.AddMessageBroker(
    builder.Configuration,
    typeof(ExpenseCreatedIntegrationEventConsumer).Assembly);

var host = builder.Build();
host.Run();
