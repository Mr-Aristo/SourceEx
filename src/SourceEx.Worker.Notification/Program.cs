using BuildingBlocks.Messaging;
using BuildingBlocks.Observability;
using SourceEx.Worker.Notification.Consumers;

var builder = Host.CreateApplicationBuilder(args);

builder.Logging.Configure(options =>
{
    options.ActivityTrackingOptions =
        ActivityTrackingOptions.TraceId |
        ActivityTrackingOptions.SpanId |
        ActivityTrackingOptions.ParentId;
});

builder.AddSourceExStructuredLogging("sourceex-worker-notification");
builder.Services.AddMessageBroker(builder.Configuration, typeof(ExpenseApprovedIntegrationEventConsumer).Assembly);

var host = builder.Build();
host.Run();
