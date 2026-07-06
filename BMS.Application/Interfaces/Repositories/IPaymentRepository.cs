using BMS.Application.DTOs.Payments;

namespace BMS.Application.Interfaces.Repositories;

public interface IPaymentRepository
{
    Task<PaymentDto?>             GetByIdAsync(int id);
    Task<IEnumerable<PaymentDto>> GetAllAsync();
    Task<IEnumerable<PaymentDto>> GetByInvoiceIdAsync(int invoiceId);
    /// <summary>Returns payments for invoices on leases belonging to tenants linked to this user.</summary>
    Task<IEnumerable<PaymentDto>> GetByUserIdAsync(string userId);
    Task<IEnumerable<PaymentDto>> GetByTenantIdAsync(int tenantId);
    Task<int?>                    GetTenantIdForPaymentAsync(int paymentId);
    Task<PaymentDto>              CreateAsync(CreatePaymentDto dto);
    Task<bool>                    ExistsAsync(int id);
}
