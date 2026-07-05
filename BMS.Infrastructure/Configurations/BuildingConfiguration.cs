using BMS.Infrastructure.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace BMS.Infrastructure.Configurations;

public class BuildingConfiguration : IEntityTypeConfiguration<Building>
{
    public void Configure(EntityTypeBuilder<Building> builder)
    {
        builder.HasKey(b => b.Id);

        builder.Property(b => b.Name)
               .IsRequired()
               .HasMaxLength(200);

        builder.Property(b => b.Address)
               .IsRequired()
               .HasMaxLength(500);

        builder.HasMany(b => b.Floors)
               .WithOne(f => f.Building)
               .HasForeignKey(f => f.BuildingId)
               .OnDelete(DeleteBehavior.Cascade);
    }
}
