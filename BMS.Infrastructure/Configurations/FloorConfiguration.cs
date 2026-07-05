using BMS.Infrastructure.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace BMS.Infrastructure.Configurations;

public class FloorConfiguration : IEntityTypeConfiguration<Floor>
{
    public void Configure(EntityTypeBuilder<Floor> builder)
    {
        builder.HasKey(f => f.Id);

        builder.Property(f => f.Label)
               .IsRequired()
               .HasMaxLength(50);

        builder.HasIndex(f => new { f.BuildingId, f.FloorNumber })
               .IsUnique();

        builder.HasMany(f => f.Units)
               .WithOne(u => u.Floor)
               .HasForeignKey(u => u.FloorId)
               .OnDelete(DeleteBehavior.SetNull);
    }
}
