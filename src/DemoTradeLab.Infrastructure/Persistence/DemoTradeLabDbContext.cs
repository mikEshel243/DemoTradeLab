using DemoTradeLab.Core.DemoProfiles;
using DemoTradeLab.Core.Orders;
using DemoTradeLab.Core.Reservations;
using DemoTradeLab.Core.Trades;
using Microsoft.EntityFrameworkCore;

namespace DemoTradeLab.Infrastructure.Persistence;

public sealed class DemoTradeLabDbContext(DbContextOptions<DemoTradeLabDbContext> options)
    : DbContext(options)
{
    public DbSet<DemoProfile> DemoProfiles => Set<DemoProfile>();

    public DbSet<DemoAccount> DemoAccounts => Set<DemoAccount>();

    public DbSet<DemoReservation> DemoReservations => Set<DemoReservation>();

    public DbSet<DemoOrder> DemoOrders => Set<DemoOrder>();

    public DbSet<DemoOrderEvent> DemoOrderEvents => Set<DemoOrderEvent>();

    public DbSet<ReservationIdempotencyRecord> ReservationIdempotencyRecords =>
        Set<ReservationIdempotencyRecord>();

    public DbSet<ReservationAuditEntry> ReservationAuditEntries =>
        Set<ReservationAuditEntry>();

    public DbSet<ReservationCompletionRecord> ReservationCompletionRecords =>
        Set<ReservationCompletionRecord>();

    public DbSet<Trade> Trades => Set<Trade>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(DemoTradeLabDbContext).Assembly);
    }
}
