using BMS.Infrastructure.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace BMS.Infrastructure.Configurations;

public class UnitConfiguration : IEntityTypeConfiguration<Unit>
{
    public void Configure(EntityTypeBuilder<Unit> builder)
    {
        builder.HasKey(u => u.Id);

        builder.Property(u => u.UnitNumber)
               .IsRequired()
               .HasMaxLength(20);

        builder.Property(u => u.AreaSqMeters)
               .HasColumnType("decimal(10,2)");

        builder.Property(u => u.MonthlyRent)
               .HasColumnType("decimal(18,2)");

        builder.Property(u => u.Description)
               .HasMaxLength(500);

        builder.HasMany(u => u.Leases)
               .WithOne(l => l.Unit)
               .HasForeignKey(l => l.UnitId)
               .OnDelete(DeleteBehavior.Restrict);
    }
}
