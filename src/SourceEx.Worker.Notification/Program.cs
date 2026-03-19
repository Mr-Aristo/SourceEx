using BuildingBlocks.Messaging;
using SourceEx.Worker.Notification.Consumers;

var builder = Host.CreateApplicationBuilder(args);

builder.Services.AddMessageBroker(builder.Configuration, typeof(ExpenseApprovedIntegrationEventConsumer).Assembly);

var host = builder.Build();
host.Run();
