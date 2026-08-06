using DemoTradeLab.Core.DemoProfiles;
using DemoTradeLab.Core.Reservations;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace DemoTradeLab.Infrastructure.Persistence.Configurations;

internal sealed class ReservationCompletionRecordConfiguration
    : IEntityTypeConfiguration<ReservationCompletionRecord>
{
    public void Configure(EntityTypeBuilder<ReservationCompletionRecord> builder)
    {
        builder.ToTable("ReservationCompletionRecords");

        builder.HasKey(record => record.Id);
        builder.Property(record => record.Id).ValueGeneratedNever();

        builder.Property(record => record.Key)
            .HasMaxLength(ReservationCompletionRecord.MaximumKeyLength)
            .IsRequired();

        builder.Property(record => record.Operation)
            .HasConversion<string>()
            .HasMaxLength(16)
            .IsRequired();

        builder.Property(record => record.CompletedAtUtc).IsRequired();

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
