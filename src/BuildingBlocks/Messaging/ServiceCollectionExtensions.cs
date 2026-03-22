using System.Reflection;
using MassTransit;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace BuildingBlocks.Messaging;

/// <summary>
/// Provides reusable message broker registration helpers.
/// </summary>
public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddMessageBroker(
        this IServiceCollection services,
        IConfiguration configuration,
        params Assembly[] consumerAssemblies)
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentNullException.ThrowIfNull(configuration);

        services.AddMassTransit(configurator =>
        {
            configurator.SetKebabCaseEndpointNameFormatter();

            foreach (var assembly in consumerAssemblies.Distinct())
            {
                configurator.AddConsumers(assembly);
            }

            configurator.UsingRabbitMq((context, busConfigurator) =>
            {
                var host = configuration["MessageBroker:Host"] ?? "localhost";
                var virtualHost = configuration["MessageBroker:VirtualHost"] ?? "/";
                var username = configuration["MessageBroker:Username"] ?? "guest";
                var password = configuration["MessageBroker:Password"] ?? "guest";

                if (ushort.TryParse(configuration["MessageBroker:Port"], out var port) && port > 0)
                {
                    busConfigurator.Host(host, port, virtualHost, hostConfigurator =>
                    {
                        hostConfigurator.Username(username);
                        hostConfigurator.Password(password);
                    });
                }
                else
                {
                    busConfigurator.Host(host, virtualHost, hostConfigurator =>
                    {
                        hostConfigurator.Username(username);
                        hostConfigurator.Password(password);
                    });
                }

                busConfigurator.UseMessageRetry(retryConfigurator =>
                {
                    retryConfigurator.Interval(3, TimeSpan.FromSeconds(5));
                });

                busConfigurator.ConfigureEndpoints(context);
            });
        });

        return services;
    }
}
