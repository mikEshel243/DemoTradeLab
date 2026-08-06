namespace DemoTradeLab.Core.Reservations;

public interface IAccountLockManager
{
    Task<IAccountLockLease> AcquireAsync(
        Guid accountId,
        CancellationToken cancellationToken);
}

public interface IAccountLockLease : IAsyncDisposable;
