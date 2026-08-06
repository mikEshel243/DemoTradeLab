using DemoTradeLab.Core.DemoProfiles;
using DemoTradeLab.Core.Reservations;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace DemoTradeLab.Infrastructure.Persistence.Configurations;

internal sealed class ReservationAuditEntryConfiguration
    : IEntityTypeConfiguration<ReservationAuditEntry>
{
    public void Configure(EntityTypeBuilder<ReservationAuditEntry> builder)
    {
        builder.ToTable("ReservationAuditEntries");

        builder.HasKey(entry => entry.Id);
        builder.Property(entry => entry.Id).ValueGeneratedNever();

        builder.Property(entry => entry.EventType)
            .HasConversion<string>()
            .HasMaxLength(40)
            .IsRequired();

        builder.Property(entry => entry.Amount)
            .HasPrecision(18, 8)
            .IsRequired();

        builder.Property(entry => entry.OccurredAtUtc).IsRequired();

        builder.HasOne<DemoAccount>()
            .WithMany()
            .HasForeignKey(entry => entry.DemoAccountId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne<DemoReservation>()
            .WithMany()
            .HasForeignKey(entry => entry.ReservationId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasIndex(entry => new
        {
            entry.DemoAccountId,
            entry.OccurredAtUtc
        });
    }
}
