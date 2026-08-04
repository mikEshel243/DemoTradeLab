using DemoTradeLab.Core.Trades;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace DemoTradeLab.Infrastructure.Persistence.Configurations;

internal sealed class TradeConfiguration : IEntityTypeConfiguration<Trade>
{
    public void Configure(EntityTypeBuilder<Trade> builder)
    {
        builder.ToTable("Trades");

        builder.HasKey(trade => trade.Id);
        builder.Property(trade => trade.Id).ValueGeneratedNever();

        builder.Property(trade => trade.Instrument)
            .HasMaxLength(100)
            .IsRequired();

        builder.Property(trade => trade.Direction)
            .HasConversion<string>()
            .HasMaxLength(4)
            .IsRequired();

        builder.Property(trade => trade.OpenedAtUtc).IsRequired();
        builder.Property(trade => trade.ClosedAtUtc).IsRequired();

        builder.Property(trade => trade.OpeningPrice)
            .HasPrecision(18, 8)
            .IsRequired();

        builder.Property(trade => trade.ClosingPrice)
            .HasPrecision(18, 8)
            .IsRequired();

        builder.Property(trade => trade.Quantity)
            .HasPrecision(18, 8)
            .IsRequired();

        builder.Property(trade => trade.RealizedProfitLoss)
            .HasPrecision(18, 8)
            .IsRequired();

        builder.Property(trade => trade.Currency)
            .HasMaxLength(3)
            .IsRequired();

        builder.Property(trade => trade.Fees).HasPrecision(18, 8);
        builder.Property(trade => trade.FinancingCosts).HasPrecision(18, 8);

        builder.Property(trade => trade.Source)
            .HasConversion<string>()
            .HasMaxLength(16)
            .IsRequired();

        builder.HasIndex(trade => trade.Instrument);
        builder.HasIndex(trade => trade.ClosedAtUtc);
    }
}
