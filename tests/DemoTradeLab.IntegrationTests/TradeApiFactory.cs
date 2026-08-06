using DemoTradeLab.Infrastructure.Persistence;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace DemoTradeLab.IntegrationTests;

public sealed class TradeApiFactory : WebApplicationFactory<Program>
{
    private readonly Action<IServiceCollection>? _configureTestServices;
    private readonly string _databasePath = Path.Combine(
        Path.GetTempPath(),
        $"demotrade-lab-api-tests-{Guid.NewGuid():N}.db");

    public TradeApiFactory(Action<IServiceCollection>? configureTestServices = null)
    {
        _configureTestServices = configureTestServices;
    }

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Testing");
        builder.ConfigureServices(services =>
        {
            services.RemoveAll<DemoTradeLabDbContext>();
            services.RemoveAll<DbContextOptions<DemoTradeLabDbContext>>();
            services.RemoveAll<IDbContextOptionsConfiguration<DemoTradeLabDbContext>>();

            services.AddDbContext<DemoTradeLabDbContext>(options =>
                options.UseSqlite($"Data Source={_databasePath}"));

            _configureTestServices?.Invoke(services);
        });
    }

    public async Task InitializeDatabaseAsync()
    {
        await using var scope = Services.CreateAsyncScope();
        var context = scope.ServiceProvider.GetRequiredService<DemoTradeLabDbContext>();
        await context.Database.MigrateAsync();
    }

    public void DisposeTestResources()
    {
        Dispose();
        SqliteConnection.ClearAllPools();
        File.Delete(_databasePath);
    }
}
