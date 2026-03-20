using BuildingBlocks.Messaging;
using SourceEx.Worker.Audit.Consumers;

var builder = Host.CreateApplicationBuilder(args);

builder.Logging.Configure(options =>
{
    options.ActivityTrackingOptions =
        ActivityTrackingOptions.TraceId |
        ActivityTrackingOptions.SpanId |
        ActivityTrackingOptions.ParentId;
});

builder.Services.AddMessageBroker(
    builder.Configuration,
    typeof(ExpenseCreatedIntegrationEventConsumer).Assembly);

var host = builder.Build();
host.Run();
