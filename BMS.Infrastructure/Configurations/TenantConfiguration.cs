using BMS.Infrastructure.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace BMS.Infrastructure.Configurations;

public class TenantConfiguration : IEntityTypeConfiguration<Tenant>
{
    public void Configure(EntityTypeBuilder<Tenant> builder)
    {
        builder.HasKey(t => t.Id);

        // Nullable FK to AppUser — set null if the owning user is deleted
        // (preserves tenant/lease/invoice history).
        // No uniqueness constraint — one user can own many tenants.
        builder.Property(t => t.AppUserId)
               .IsRequired(false)
               .HasMaxLength(450); // Identity ID is a GUID string

        builder.HasOne(t => t.AppUser)
               .WithMany(u => u.Tenants)
               .HasForeignKey(t => t.AppUserId)
               .OnDelete(DeleteBehavior.SetNull);

        builder.Property(t => t.OrganizationName)
               .IsRequired()
               .HasMaxLength(200);

        builder.Property(t => t.TIN)
               .IsRequired()
               .HasMaxLength(50);

        builder.Property(t => t.Phone)
               .IsRequired()
               .HasMaxLength(20);

        builder.Property(t => t.ContactPersonName)
               .IsRequired()
               .HasMaxLength(100);

        builder.Property(t => t.ContactEmail)
               .IsRequired()
               .HasMaxLength(150);

        builder.HasMany(t => t.Leases)
               .WithOne(l => l.Tenant)
               .HasForeignKey(l => l.TenantId)
               .OnDelete(DeleteBehavior.Restrict);

        builder.HasMany(t => t.LegalDocuments)
               .WithOne(d => d.Tenant)
               .HasForeignKey(d => d.TenantId)
               .OnDelete(DeleteBehavior.Cascade);
    }
}
