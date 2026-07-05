namespace BMS.Application.DTOs.Payments;

public class PaymentDto
{
    public int      Id              { get; set; }
    public int      InvoiceId       { get; set; }
    public string   InvoiceNumber   { get; set; } = string.Empty;
    public decimal  AmountPaid      { get; set; }
    public DateTime PaymentDate     { get; set; }
    public string   PaymentMethod   { get; set; } = string.Empty;
    public string   ReferenceNumber { get; set; } = string.Empty;
    public string?  Notes           { get; set; }
}

public class CreatePaymentDto
{
    public int      InvoiceId       { get; set; }
    public decimal  AmountPaid      { get; set; }
    public DateTime PaymentDate     { get; set; }
    public string   PaymentMethod   { get; set; } = string.Empty;
    public string   ReferenceNumber { get; set; } = string.Empty;
    public string?  Notes           { get; set; }
}
