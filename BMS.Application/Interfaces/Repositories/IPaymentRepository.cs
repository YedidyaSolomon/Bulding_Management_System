using BMS.Application.DTOs.Payments;

namespace BMS.Application.Interfaces.Repositories;

public interface IPaymentRepository
{
    Task<PaymentDto?>             GetByIdAsync(int id);
    Task<IEnumerable<PaymentDto>> GetAllAsync();
    Task<IEnumerable<PaymentDto>> GetByInvoiceIdAsync(int invoiceId);
    Task<PaymentDto>              CreateAsync(CreatePaymentDto dto);
    Task<bool>                    ExistsAsync(int id);
}
