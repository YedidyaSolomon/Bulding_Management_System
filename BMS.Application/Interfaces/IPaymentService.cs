using BMS.Application.DTOs.Payments;

namespace BMS.Application.Interfaces;

public interface IPaymentService
{
    Task<IEnumerable<PaymentDto>> GetAllAsync();
    Task<PaymentDto>              GetByIdAsync(int id);
    Task<IEnumerable<PaymentDto>> GetByInvoiceIdAsync(int invoiceId);
    Task<PaymentDto>              RecordPaymentAsync(CreatePaymentDto dto);
}
