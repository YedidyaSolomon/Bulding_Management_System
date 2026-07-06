using BMS.Application.DTOs.Payments;

namespace BMS.Application.Interfaces;

public interface IPaymentService
{
    Task<IEnumerable<PaymentDto>> GetAllAsync();
    Task<PaymentDto>              GetByIdAsync(int id);
    Task<IEnumerable<PaymentDto>> GetByInvoiceIdAsync(int invoiceId);
    /// <summary>Returns only the payments for invoices on leases owned by this user.</summary>
    Task<IEnumerable<PaymentDto>> GetByUserIdAsync(string userId);
    Task<PaymentDto>              RecordPaymentAsync(CreatePaymentDto dto);
}
