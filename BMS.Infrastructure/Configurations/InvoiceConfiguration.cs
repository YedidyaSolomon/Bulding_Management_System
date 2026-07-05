using BMS.Infrastructure.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace BMS.Infrastructure.Configurations;

public class InvoiceConfiguration : IEntityTypeConfiguration<Invoice>
{
    public void Configure(EntityTypeBuilder<Invoice> builder)
    {
        builder.HasKey(i => i.Id);

        builder.Property(i => i.InvoiceNumber)
               .IsRequired()
               .HasMaxLength(50);

        builder.HasIndex(i => i.InvoiceNumber)
               .IsUnique();

        builder.Property(i => i.AmountDue)
               .HasColumnType("decimal(18,2)");

        builder.HasMany(i => i.Payments)
               .WithOne(p => p.Invoice)
               .HasForeignKey(p => p.InvoiceId)
               .OnDelete(DeleteBehavior.Restrict);
    }
}
