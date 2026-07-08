namespace BMS.Application.DTOs.Invoices;

public class InvoiceDto
{
    public int      Id            { get; set; }
    public int      LeaseId       { get; set; }
    public string   InvoiceNumber { get; set; } = string.Empty;
    public decimal  AmountDue     { get; set; }
    public DateTime DueDate       { get; set; }
    public DateTime IssueDate     { get; set; }
    public string   Status        { get; set; } = string.Empty;
    public int      PeriodMonth   { get; set; }
    public int      PeriodYear    { get; set; }

    // Denormalised display fields — populated from Lease.Tenant and Lease.Unit
    public int      TenantId      { get; set; }
    public string   TenantName    { get; set; } = string.Empty;
    public int      UnitId        { get; set; }
    public string   UnitNumber    { get; set; } = string.Empty;
}

public class CreateInvoiceDto
{
    public int      LeaseId     { get; set; }
    public decimal  AmountDue   { get; set; }
    public DateTime DueDate     { get; set; }
    public int      PeriodMonth { get; set; }
    public int      PeriodYear  { get; set; }
}
