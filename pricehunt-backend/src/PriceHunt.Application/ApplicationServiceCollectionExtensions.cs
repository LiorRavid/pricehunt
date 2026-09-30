using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using PriceHunt.Application.Searches;

namespace PriceHunt.Application;

/// <summary>Registers the application's use cases.</summary>
public static class ApplicationServiceCollectionExtensions
{
    /// <summary>Adds the search use cases and the system clock, unless another clock is registered.</summary>
    /// <param name="services">The service collection.</param>
    /// <returns>The same service collection.</returns>
    public static IServiceCollection AddApplication(this IServiceCollection services)
    {
        ArgumentNullException.ThrowIfNull(services);

        services.TryAddSingleton(TimeProvider.System);
        services.AddScoped<SearchPlanner>();
        services.AddScoped<SearchOrchestrator>();
        services.AddScoped<InterruptedSearchRecovery>();
        return services;
    }
}
