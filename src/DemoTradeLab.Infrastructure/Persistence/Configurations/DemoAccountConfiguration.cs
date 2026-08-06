using DemoTradeLab.Core.DemoProfiles;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace DemoTradeLab.Infrastructure.Persistence.Configurations;

internal sealed class DemoAccountConfiguration : IEntityTypeConfiguration<DemoAccount>
{
    public void Configure(EntityTypeBuilder<DemoAccount> builder)
    {
        builder.ToTable("DemoAccounts");

        builder.HasKey(account => account.Id);
        builder.Property(account => account.Id).ValueGeneratedNever();

        builder.Property(account => account.Key)
            .HasMaxLength(50)
            .IsRequired();

        builder.Property(account => account.DisplayName)
            .HasMaxLength(100)
            .IsRequired();

        builder.Property(account => account.TotalBalance)
            .HasPrecision(18, 8)
            .IsRequired();

        builder.Property(account => account.ReservedBalance)
            .HasPrecision(18, 8)
            .IsRequired();

        builder.Property(account => account.Currency)
            .HasMaxLength(3)
            .IsRequired();

        builder.Ignore(account => account.AvailableBalance);

        builder.HasIndex(account => new { account.DemoProfileId, account.Key })
            .IsUnique();
    }
}
