namespace DemoTradeLab.Core.Trades;

public sealed class TradeService(ITradeRepository repository)
{
    public Task<IReadOnlyList<Trade>> ListAsync(CancellationToken cancellationToken) =>
        repository.ListAsync(cancellationToken);

    public Task<Trade?> GetByIdAsync(Guid id, CancellationToken cancellationToken) =>
        repository.GetByIdAsync(id, cancellationToken);

    public async Task<TradeCreationResult> CreateAsync(
        TradeDraft draft,
        CancellationToken cancellationToken)
    {
        var result = Trade.Create(draft);

        if (!result.IsSuccess)
        {
            return result;
        }

        repository.Add(result.Trade);
        await repository.SaveChangesAsync(cancellationToken);

        return result;
    }

    public async Task<TradeUpdateResult?> UpdateAsync(
        Guid id,
        TradeDraft draft,
        CancellationToken cancellationToken)
    {
        var trade = await repository.GetByIdForUpdateAsync(id, cancellationToken);

        if (trade is null)
        {
            return null;
        }

        var effectiveDraft = draft with
        {
            Source = trade.Source,
            ImportedAtUtc = trade.ImportedAtUtc
        };
        var result = trade.Update(effectiveDraft);

        if (result.IsSuccess)
        {
            await repository.SaveChangesAsync(cancellationToken);
        }

        return result;
    }

    public async Task<bool> DeleteAsync(Guid id, CancellationToken cancellationToken)
    {
        var trade = await repository.GetByIdForUpdateAsync(id, cancellationToken);

        if (trade is null)
        {
            return false;
        }

        repository.Remove(trade);
        await repository.SaveChangesAsync(cancellationToken);

        return true;
    }
}
