using DemoTradeLab.Core.DemoProfiles;
using DemoTradeLab.Core.Reservations;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace DemoTradeLab.Infrastructure.Persistence.Configurations;

internal sealed class DemoReservationConfiguration
    : IEntityTypeConfiguration<DemoReservation>
{
    public void Configure(EntityTypeBuilder<DemoReservation> builder)
    {
        builder.ToTable("DemoReservations");

        builder.HasKey(reservation => reservation.Id);
        builder.Property(reservation => reservation.Id).ValueGeneratedNever();

        builder.Property(reservation => reservation.Amount)
            .HasPrecision(18, 8)
            .IsRequired();

        builder.Property(reservation => reservation.Currency)
            .HasMaxLength(3)
            .IsRequired();

        builder.Property(reservation => reservation.Status)
            .HasConversion<string>()
            .HasMaxLength(16)
            .IsRequired();

        builder.Property(reservation => reservation.CreatedAtUtc).IsRequired();

        builder.HasOne<DemoAccount>()
            .WithMany()
            .HasForeignKey(reservation => reservation.DemoAccountId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasIndex(reservation => reservation.DemoAccountId);
        builder.HasIndex(reservation => new
        {
            reservation.DemoAccountId,
            reservation.CreatedAtUtc
        });
    }
}
