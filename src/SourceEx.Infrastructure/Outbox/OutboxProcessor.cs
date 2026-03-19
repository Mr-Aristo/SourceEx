using System.Text.Json;
using MassTransit;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using SourceEx.Infrastructure.Data.Context;

namespace SourceEx.Infrastructure.Outbox;

/// <summary>
/// Publishes pending outbox messages to the message broker.
/// </summary>
public sealed class OutboxProcessor : BackgroundService
{
    private static readonly TimeSpan PollingInterval = TimeSpan.FromSeconds(10);
    private readonly IServiceScopeFactory _serviceScopeFactory;
    private readonly ILogger<OutboxProcessor> _logger;

    public OutboxProcessor(IServiceScopeFactory serviceScopeFactory, ILogger<OutboxProcessor> logger)
    {
        _serviceScopeFactory = serviceScopeFactory;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        using var timer = new PeriodicTimer(PollingInterval);

        while (!stoppingToken.IsCancellationRequested)
        {
            await ProcessPendingMessagesAsync(stoppingToken);

            if (!await timer.WaitForNextTickAsync(stoppingToken))
                break;
        }
    }

    private async Task ProcessPendingMessagesAsync(CancellationToken cancellationToken)
    {
        using var scope = _serviceScopeFactory.CreateScope();

        var dbContext = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        var publishEndpoint = scope.ServiceProvider.GetRequiredService<IPublishEndpoint>();

        var messages = await dbContext.OutboxMessages
            .Where(message => message.ProcessedOnUtc == null)
            .OrderBy(message => message.OccurredOnUtc)
            .Take(20)
            .ToListAsync(cancellationToken);

        if (messages.Count == 0)
            return;

        foreach (var message in messages)
        {
            cancellationToken.ThrowIfCancellationRequested();
            message.LastAttemptOnUtc = DateTime.UtcNow;

            try
            {
                var messageType = Type.GetType(message.Type, throwOnError: false);
                if (messageType == null)
                    throw new InvalidOperationException($"Could not resolve message type '{message.Type}'.");

                var payload = JsonSerializer.Deserialize(message.Content, messageType);
                if (payload == null)
                    throw new InvalidOperationException($"Could not deserialize outbox message '{message.Id}'.");

                await publishEndpoint.Publish(payload, messageType, cancellationToken);

                message.ProcessedOnUtc = DateTime.UtcNow;
                message.Error = null;
            }
            catch (Exception exception)
            {
                message.RetryCount++;
                message.Error = exception.Message;

                _logger.LogError(
                    exception,
                    "Failed to publish outbox message {OutboxMessageId} of type {OutboxMessageType}.",
                    message.Id,
                    message.Type);
            }
        }

        await dbContext.SaveChangesAsync(cancellationToken);
    }
}
