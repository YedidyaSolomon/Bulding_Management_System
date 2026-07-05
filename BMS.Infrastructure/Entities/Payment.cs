using BMS.Infrastructure.Enums;

namespace BMS.Infrastructure.Entities;

public class Payment
{
    public int           Id              { get; set; }
    public int           InvoiceId       { get; set; }
    public decimal       AmountPaid      { get; set; }
    public DateTime      PaymentDate     { get; set; }
    public PaymentMethod PaymentMethod   { get; set; }
    public string        ReferenceNumber { get; set; } = string.Empty;
    public string?       Notes           { get; set; }

    public DateTime  CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? UpdatedAt { get; set; }

    public Invoice Invoice { get; set; } = null!;
}
