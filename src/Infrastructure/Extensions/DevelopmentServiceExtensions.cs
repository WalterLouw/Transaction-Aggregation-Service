using Application.Services;
using Infrastructure.MockTransactions;
using Microsoft.Extensions.DependencyInjection;

namespace Infrastructure.Extensions;

public static class DevelopmentServiceExtensions
{
    /// <summary>
    /// Registers development-only services.
    /// Called only when the app runs in the Development environment.
    /// </summary>
    public static IServiceCollection AddDevelopmentInfrastructure(this IServiceCollection services)
    {
        // Replace real sources with the mock source so Swagger works without
        // any real database or API connections configured
        var realSources = services
            .Where(d => d.ServiceType == typeof(ITransactionSource))
            .ToList();
 
        foreach (var s in realSources)
            services.Remove(s);
 
        services.AddScoped<ITransactionSource, MockTransactionSource>();
 
        return services;
    }
}