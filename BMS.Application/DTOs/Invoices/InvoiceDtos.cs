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
}

public class CreateInvoiceDto
{
    public int      LeaseId     { get; set; }
    public decimal  AmountDue   { get; set; }
    public DateTime DueDate     { get; set; }
    public int      PeriodMonth { get; set; }
    public int      PeriodYear  { get; set; }
}
