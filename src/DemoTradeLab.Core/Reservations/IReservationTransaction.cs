namespace DemoTradeLab.Core.Reservations;

public interface IReservationTransaction : IAsyncDisposable
{
    Task CommitAsync(CancellationToken cancellationToken);
}
