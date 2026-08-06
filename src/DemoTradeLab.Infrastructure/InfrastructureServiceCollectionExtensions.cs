using DemoTradeLab.Core.DemoProfiles;
using DemoTradeLab.Core.Trades;
using DemoTradeLab.Infrastructure.Persistence;
using DemoTradeLab.Infrastructure.Persistence.Repositories;
using DemoTradeLab.Infrastructure.Persistence.Seeding;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace DemoTradeLab.Infrastructure;

public static class InfrastructureServiceCollectionExtensions
{
    public static IServiceCollection AddInfrastructure(
        this IServiceCollection services,
        string connectionString,
        IReadOnlyList<DemoProfileSeed>? demoProfileSeeds = null)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(connectionString);
        var seedDefinitions = demoProfileSeeds?.ToArray() ?? [];

        services.AddDbContext<DemoTradeLabDbContext>(options =>
            options
                .UseSqlite(connectionString)
                .UseSeeding((context, _) =>
                    DemoTradeLabDataSeeder.Seed(context, seedDefinitions))
                .UseAsyncSeeding((context, _, cancellationToken) =>
                    DemoTradeLabDataSeeder.SeedAsync(
                        context,
                        seedDefinitions,
                        cancellationToken)));

        services.AddScoped<IDemoProfileRepository, EfDemoProfileRepository>();
        services.AddScoped<ITradeRepository, EfTradeRepository>();

        return services;
    }
}
