namespace DemoTradeLab.Core.Trades;

public sealed class TradeService(ITradeRepository repository)
{
    public async Task<IReadOnlyList<Trade>> ListAsync(
        TradeListQuery query,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(query);

        var trades = await repository.ListAsync(cancellationToken);
        var filteredTrades = ApplyFilters(trades, query);
        var sortedTrades = ApplySorting(filteredTrades, query);

        return sortedTrades.ThenBy(trade => trade.Id).ToArray();
    }

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

    private static IEnumerable<Trade> ApplyFilters(
        IEnumerable<Trade> trades,
        TradeListQuery query)
    {
        if (!string.IsNullOrWhiteSpace(query.Instrument))
        {
            var instrument = query.Instrument.Trim();
            trades = trades.Where(trade =>
                trade.Instrument.Equals(instrument, StringComparison.OrdinalIgnoreCase));
        }

        if (!string.IsNullOrWhiteSpace(query.Currency))
        {
            var currency = query.Currency.Trim();
            trades = trades.Where(trade =>
                trade.Currency.Equals(currency, StringComparison.OrdinalIgnoreCase));
        }

        if (query.Direction is { } direction)
        {
            trades = trades.Where(trade => trade.Direction == direction);
        }

        if (query.Source is { } source)
        {
            trades = trades.Where(trade => trade.Source == source);
        }

        if (query.Outcome is { } outcome)
        {
            trades = trades.Where(trade => MatchesOutcome(trade, outcome));
        }

        if (query.ClosedFromUtc is { } closedFromUtc)
        {
            trades = trades.Where(trade => trade.ClosedAtUtc >= closedFromUtc);
        }

        if (query.ClosedToUtc is { } closedToUtc)
        {
            trades = trades.Where(trade => trade.ClosedAtUtc <= closedToUtc);
        }

        return trades;
    }

    private static IOrderedEnumerable<Trade> ApplySorting(
        IEnumerable<Trade> trades,
        TradeListQuery query) =>
        (query.SortBy, query.SortDirection) switch
        {
            (TradeSortField.ClosedAtUtc, TradeSortDirection.Ascending) =>
                trades.OrderBy(trade => trade.ClosedAtUtc),
            (TradeSortField.ClosedAtUtc, TradeSortDirection.Descending) =>
                trades.OrderByDescending(trade => trade.ClosedAtUtc),
            (TradeSortField.OpenedAtUtc, TradeSortDirection.Ascending) =>
                trades.OrderBy(trade => trade.OpenedAtUtc),
            (TradeSortField.OpenedAtUtc, TradeSortDirection.Descending) =>
                trades.OrderByDescending(trade => trade.OpenedAtUtc),
            (TradeSortField.Instrument, TradeSortDirection.Ascending) =>
                trades.OrderBy(
                    trade => trade.Instrument,
                    StringComparer.OrdinalIgnoreCase),
            (TradeSortField.Instrument, TradeSortDirection.Descending) =>
                trades.OrderByDescending(
                    trade => trade.Instrument,
                    StringComparer.OrdinalIgnoreCase),
            (TradeSortField.RealizedProfitLoss, TradeSortDirection.Ascending) =>
                trades.OrderBy(trade => trade.RealizedProfitLoss),
            (TradeSortField.RealizedProfitLoss, TradeSortDirection.Descending) =>
                trades.OrderByDescending(trade => trade.RealizedProfitLoss),
            (TradeSortField.Duration, TradeSortDirection.Ascending) =>
                trades.OrderBy(trade => trade.ClosedAtUtc - trade.OpenedAtUtc),
            (TradeSortField.Duration, TradeSortDirection.Descending) =>
                trades.OrderByDescending(trade => trade.ClosedAtUtc - trade.OpenedAtUtc),
            _ => throw new ArgumentOutOfRangeException(
                nameof(query),
                "The requested trade sorting is not supported.")
        };

    private static bool MatchesOutcome(Trade trade, TradeOutcome outcome) =>
        outcome switch
        {
            TradeOutcome.Profitable => trade.RealizedProfitLoss > 0m,
            TradeOutcome.Losing => trade.RealizedProfitLoss < 0m,
            TradeOutcome.BreakEven => trade.RealizedProfitLoss == 0m,
            _ => throw new ArgumentOutOfRangeException(
                nameof(outcome),
                outcome,
                "The requested trade outcome is not supported.")
        };
}
