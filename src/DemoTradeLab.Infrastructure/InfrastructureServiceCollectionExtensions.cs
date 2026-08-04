using DemoTradeLab.Infrastructure.Persistence;
using DemoTradeLab.Infrastructure.Persistence.Seeding;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace DemoTradeLab.Infrastructure;

public static class InfrastructureServiceCollectionExtensions
{
    public static IServiceCollection AddInfrastructure(
        this IServiceCollection services,
        string connectionString)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(connectionString);

        services.AddDbContext<DemoTradeLabDbContext>(options =>
            options
                .UseSqlite(connectionString)
                .UseSeeding((context, _) =>
                    DemoTradeLabDataSeeder.Seed(context))
                .UseAsyncSeeding((context, _, cancellationToken) =>
                    DemoTradeLabDataSeeder.SeedAsync(context, cancellationToken)));

        return services;
    }
}
