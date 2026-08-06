using DemoTradeLab.Core.DemoProfiles;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace DemoTradeLab.Infrastructure.Persistence.Configurations;

internal sealed class DemoProfileConfiguration : IEntityTypeConfiguration<DemoProfile>
{
    public void Configure(EntityTypeBuilder<DemoProfile> builder)
    {
        builder.ToTable("DemoProfiles");

        builder.HasKey(profile => profile.Id);
        builder.Property(profile => profile.Id).ValueGeneratedNever();

        builder.Property(profile => profile.Key)
            .HasMaxLength(50)
            .IsRequired();

        builder.Property(profile => profile.DisplayName)
            .HasMaxLength(100)
            .IsRequired();

        builder.HasIndex(profile => profile.Key).IsUnique();

        builder.HasMany(profile => profile.Accounts)
            .WithOne()
            .HasForeignKey(account => account.DemoProfileId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.Navigation(profile => profile.Accounts)
            .UsePropertyAccessMode(PropertyAccessMode.Field);
    }
}
