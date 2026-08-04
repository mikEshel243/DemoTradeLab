using DemoTradeLab.Core.Trades;
using Microsoft.EntityFrameworkCore;

namespace DemoTradeLab.Infrastructure.Persistence.Seeding;

internal static class DemoTradeLabDataSeeder
{
    public static void Seed(DbContext context)
    {
        if (context.Set<Trade>().Any())
        {
            return;
        }

        context.Set<Trade>().AddRange(CreateTrades());
        context.SaveChanges();
    }

    public static async Task SeedAsync(
        DbContext context,
        CancellationToken cancellationToken)
    {
        if (await context.Set<Trade>().AnyAsync(cancellationToken))
        {
            return;
        }

        context.Set<Trade>().AddRange(CreateTrades());
        await context.SaveChangesAsync(cancellationToken);
    }

    private static IReadOnlyList<Trade> CreateTrades() =>
        SampleTradeData.CreateDrafts()
            .Select(CreateTrade)
            .ToArray();

    private static Trade CreateTrade(TradeDraft draft)
    {
        var result = Trade.Create(draft);

        if (result.Trade is { } trade)
        {
            return trade;
        }

        var errorSummary = string.Join(
            "; ",
            result.Errors.Select(error => $"{error.PropertyName}: {error.Message}"));

        throw new InvalidOperationException(
            $"Fictional sample trade configuration is invalid. {errorSummary}");
    }
}
