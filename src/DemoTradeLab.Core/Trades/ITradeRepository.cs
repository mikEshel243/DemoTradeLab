namespace DemoTradeLab.Core.Trades;

public interface ITradeRepository
{
    Task<IReadOnlyList<Trade>> ListAsync(CancellationToken cancellationToken);

    Task<Trade?> GetByIdAsync(Guid id, CancellationToken cancellationToken);

    Task<Trade?> GetByIdForUpdateAsync(Guid id, CancellationToken cancellationToken);

    void Add(Trade trade);

    void Remove(Trade trade);

    Task SaveChangesAsync(CancellationToken cancellationToken);
}
