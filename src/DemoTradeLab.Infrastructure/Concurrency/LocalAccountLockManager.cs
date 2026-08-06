using System.Diagnostics;
using DemoTradeLab.Core.Reservations;
using Microsoft.Extensions.Logging;

namespace DemoTradeLab.Infrastructure.Concurrency;

internal sealed class LocalAccountLockManager(
    ILogger<LocalAccountLockManager> logger) : IAccountLockManager
{
    private readonly object _gate = new();
    private readonly Dictionary<Guid, LockEntry> _entries = [];

    public async Task<IAccountLockLease> AcquireAsync(
        Guid accountId,
        CancellationToken cancellationToken)
    {
        LockEntry entry;

        lock (_gate)
        {
            if (!_entries.TryGetValue(accountId, out entry!))
            {
                entry = new LockEntry();
                _entries.Add(accountId, entry);
            }

            entry.ReferenceCount++;
        }

        var stopwatch = Stopwatch.StartNew();

        try
        {
            await entry.Semaphore.WaitAsync(cancellationToken);
        }
        catch
        {
            RemoveReference(accountId, entry);
            throw;
        }

        logger.LogDebug(
            "Acquired local lock for demo account {AccountId} after {WaitMilliseconds} ms",
            accountId,
            stopwatch.ElapsedMilliseconds);

        return new AccountLockLease(this, accountId, entry);
    }

    private void Release(Guid accountId, LockEntry entry)
    {
        entry.Semaphore.Release();
        RemoveReference(accountId, entry);
    }

    private void RemoveReference(Guid accountId, LockEntry entry)
    {
        lock (_gate)
        {
            entry.ReferenceCount--;

            if (entry.ReferenceCount != 0)
            {
                return;
            }

            _entries.Remove(accountId);
            entry.Semaphore.Dispose();
        }
    }

    private sealed class LockEntry
    {
        public SemaphoreSlim Semaphore { get; } = new(1, 1);

        public int ReferenceCount { get; set; }
    }

    private sealed class AccountLockLease(
        LocalAccountLockManager owner,
        Guid accountId,
        LockEntry entry) : IAccountLockLease
    {
        private LocalAccountLockManager? _owner = owner;

        public ValueTask DisposeAsync()
        {
            Interlocked.Exchange(ref _owner, null)?.Release(accountId, entry);
            return ValueTask.CompletedTask;
        }
    }
}
