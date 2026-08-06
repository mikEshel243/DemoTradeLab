using DemoTradeLab.Core.Reservations;
using Microsoft.EntityFrameworkCore.Storage;

namespace DemoTradeLab.Infrastructure.Persistence;

internal sealed class EfReservationTransaction(IDbContextTransaction transaction)
    : IReservationTransaction
{
    public Task CommitAsync(CancellationToken cancellationToken) =>
        transaction.CommitAsync(cancellationToken);

    public ValueTask DisposeAsync() => transaction.DisposeAsync();
}
