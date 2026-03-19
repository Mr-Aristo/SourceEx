using FluentValidation;
using MediatR;
using Microsoft.Extensions.DependencyInjection;
using SourceEx.Application.Behaviors;

namespace SourceEx.Application;

/// <summary>
/// Registers application services.
/// </summary>
public static class DependencyInjection
{
    public static IServiceCollection AddApplication(this IServiceCollection services)
    {
        ValidatorOptions.Global.DefaultRuleLevelCascadeMode = CascadeMode.Stop;

        services.AddMediatR(configuration =>
        {
            configuration.RegisterServicesFromAssembly(typeof(DependencyInjection).Assembly);
        });

        services.AddValidatorsFromAssemblyContaining<DependencyInjection>(ServiceLifetime.Transient);
        services.AddTransient(typeof(IPipelineBehavior<,>), typeof(ValidationBehavior<,>));

        return services;
    }
}
