using BMS.Infrastructure.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace BMS.Infrastructure.Configurations;

public class AppUserConfiguration : IEntityTypeConfiguration<AppUser>
{
    public void Configure(EntityTypeBuilder<AppUser> builder)
    {
        builder.Property(u => u.FullName)
               .IsRequired()
               .HasMaxLength(100);

        builder.Property(u => u.IsActive)
               .IsRequired()
               .HasDefaultValue(true);

        builder.Property(u => u.CreatedAt)
               .IsRequired();

        builder.HasMany(u => u.Notifications)
               .WithOne(n => n.User)
               .HasForeignKey(n => n.UserId)
               .OnDelete(DeleteBehavior.Cascade);
    }
}
