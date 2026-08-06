using DemoTradeLab.Core.Orders;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace DemoTradeLab.Infrastructure.Persistence.Configurations;

internal sealed class DemoOrderEventConfiguration
    : IEntityTypeConfiguration<DemoOrderEvent>
{
    public void Configure(EntityTypeBuilder<DemoOrderEvent> builder)
    {
        builder.ToTable("DemoOrderEvents");

        builder.HasKey(orderEvent => orderEvent.Id);
        builder.Property(orderEvent => orderEvent.Id).ValueGeneratedNever();

        builder.Property(orderEvent => orderEvent.EventType)
            .HasConversion<string>()
            .HasMaxLength(16)
            .IsRequired();

        builder.Property(orderEvent => orderEvent.OccurredAtUtc).IsRequired();

        builder.HasOne<DemoOrder>()
            .WithMany()
            .HasForeignKey(orderEvent => orderEvent.OrderId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasIndex(orderEvent => new
        {
            orderEvent.OrderId,
            orderEvent.OccurredAtUtc
        });
    }
}
