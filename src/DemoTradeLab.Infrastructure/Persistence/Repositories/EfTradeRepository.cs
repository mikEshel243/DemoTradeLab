using DemoTradeLab.Core.Trades;
using Microsoft.EntityFrameworkCore;

namespace DemoTradeLab.Infrastructure.Persistence.Repositories;

internal sealed class EfTradeRepository(DemoTradeLabDbContext context) : ITradeRepository
{
    public async Task<IReadOnlyList<Trade>> ListAsync(CancellationToken cancellationToken) =>
        await context.Trades
            .AsNoTracking()
            .ToListAsync(cancellationToken);

    public Task<Trade?> GetByIdAsync(Guid id, CancellationToken cancellationToken) =>
        context.Trades
            .AsNoTracking()
            .SingleOrDefaultAsync(trade => trade.Id == id, cancellationToken);

    public Task<Trade?> GetByIdForUpdateAsync(
        Guid id,
        CancellationToken cancellationToken) =>
        context.Trades.SingleOrDefaultAsync(
            trade => trade.Id == id,
            cancellationToken);

    public void Add(Trade trade)
    {
        context.Trades.Add(trade);
    }

    public void Remove(Trade trade)
    {
        context.Trades.Remove(trade);
    }

    public async Task SaveChangesAsync(CancellationToken cancellationToken)
    {
        await context.SaveChangesAsync(cancellationToken);
    }
}
