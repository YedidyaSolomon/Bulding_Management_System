using BMS.Infrastructure.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace BMS.Infrastructure.Configurations;

public class LeaseConfiguration : IEntityTypeConfiguration<Lease>
{
    public void Configure(EntityTypeBuilder<Lease> builder)
    {
        builder.HasKey(l => l.Id);

        builder.Property(l => l.MonthlyRent)
               .HasColumnType("decimal(18,2)");

        builder.Property(l => l.DepositAmount)
               .HasColumnType("decimal(18,2)");

        builder.Property(l => l.TerminationReason)
               .HasMaxLength(500);

        builder.HasMany(l => l.Invoices)
               .WithOne(i => i.Lease)
               .HasForeignKey(i => i.LeaseId)
               .OnDelete(DeleteBehavior.Restrict);
    }
}
