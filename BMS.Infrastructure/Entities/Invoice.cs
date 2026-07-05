using BMS.Infrastructure.Enums;

namespace BMS.Infrastructure.Entities;

public class Invoice
{
    public int           Id            { get; set; }
    public int           LeaseId       { get; set; }
    public string        InvoiceNumber { get; set; } = string.Empty;
    public decimal       AmountDue     { get; set; }
    public DateTime      DueDate       { get; set; }
    public DateTime      IssueDate     { get; set; } = DateTime.UtcNow;
    public InvoiceStatus Status        { get; set; } = InvoiceStatus.Draft;
    public int           PeriodMonth   { get; set; }
    public int           PeriodYear    { get; set; }

    public DateTime  CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? UpdatedAt { get; set; }

    public Lease Lease { get; set; } = null!;

    public ICollection<Payment> Payments { get; set; } = new List<Payment>();
}
