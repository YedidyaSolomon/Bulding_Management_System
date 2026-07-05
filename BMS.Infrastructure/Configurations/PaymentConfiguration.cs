using BMS.Infrastructure.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace BMS.Infrastructure.Configurations;

public class PaymentConfiguration : IEntityTypeConfiguration<Payment>
{
    public void Configure(EntityTypeBuilder<Payment> builder)
    {
        builder.HasKey(p => p.Id);

        builder.Property(p => p.AmountPaid)
               .HasColumnType("decimal(18,2)");

        builder.Property(p => p.ReferenceNumber)
               .IsRequired()
               .HasMaxLength(100);

        builder.Property(p => p.Notes)
               .HasMaxLength(500);
    }
}
