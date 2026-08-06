using DemoTradeLab.Core.DemoProfiles;
using DemoTradeLab.Core.Orders;
using DemoTradeLab.Core.Reservations;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace DemoTradeLab.Infrastructure.Persistence.Configurations;

internal sealed class DemoOrderConfiguration : IEntityTypeConfiguration<DemoOrder>
{
    public void Configure(EntityTypeBuilder<DemoOrder> builder)
    {
        builder.ToTable("DemoOrders");

        builder.HasKey(order => order.Id);
        builder.Property(order => order.Id).ValueGeneratedNever();

        builder.Property(order => order.Amount)
            .HasPrecision(18, 8)
            .IsRequired();

        builder.Property(order => order.Currency)
            .HasMaxLength(3)
            .IsRequired();

        builder.Property(order => order.Status)
            .HasConversion<string>()
            .HasMaxLength(16)
            .IsRequired();

        builder.Property(order => order.CreatedAtUtc).IsRequired();
        builder.Property(order => order.UpdatedAtUtc).IsRequired();

        builder.HasOne<DemoAccount>()
            .WithMany()
            .HasForeignKey(order => order.DemoAccountId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne<DemoReservation>()
            .WithOne()
            .HasForeignKey<DemoOrder>(order => order.ReservationId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasIndex(order => order.DemoAccountId);
        builder.HasIndex(order => order.ReservationId).IsUnique();
    }
}
