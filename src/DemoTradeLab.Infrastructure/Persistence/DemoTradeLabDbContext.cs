using DemoTradeLab.Core.DemoProfiles;
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

    public DbSet<Trade> Trades => Set<Trade>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(DemoTradeLabDbContext).Assembly);
    }
}
