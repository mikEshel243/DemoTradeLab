using DemoTradeLab.Core.Trades;
using Microsoft.EntityFrameworkCore;

namespace DemoTradeLab.Infrastructure.Persistence;

public sealed class DemoTradeLabDbContext(DbContextOptions<DemoTradeLabDbContext> options)
    : DbContext(options)
{
    public DbSet<Trade> Trades => Set<Trade>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(DemoTradeLabDbContext).Assembly);
    }
}
