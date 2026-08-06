using DemoTradeLab.Core.DemoProfiles;
using DemoTradeLab.Core.Reservations;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace DemoTradeLab.Infrastructure.Persistence.Configurations;

internal sealed class ReservationIdempotencyRecordConfiguration
    : IEntityTypeConfiguration<ReservationIdempotencyRecord>
{
    public void Configure(EntityTypeBuilder<ReservationIdempotencyRecord> builder)
    {
        builder.ToTable("ReservationIdempotencyRecords");

        builder.HasKey(record => record.Id);
        builder.Property(record => record.Id).ValueGeneratedNever();

        builder.Property(record => record.Key)
            .HasMaxLength(ReservationIdempotencyRecord.MaximumKeyLength)
            .IsRequired();

        builder.Property(record => record.RequestedAmount)
            .HasPrecision(18, 8)
            .IsRequired();

        builder.Property(record => record.Outcome)
            .HasConversion<string>()
            .HasMaxLength(32)
            .IsRequired();

        builder.Property(record => record.CreatedAtUtc).IsRequired();

        builder.HasOne<DemoAccount>()
            .WithMany()
            .HasForeignKey(record => record.DemoAccountId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne<DemoReservation>()
            .WithMany()
            .HasForeignKey(record => record.ReservationId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasIndex(record => new { record.DemoAccountId, record.Key })
            .IsUnique();
    }
}
