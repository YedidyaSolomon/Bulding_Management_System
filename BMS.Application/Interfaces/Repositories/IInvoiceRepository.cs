using BMS.Application.DTOs.Invoices;

namespace BMS.Application.Interfaces.Repositories;

public interface IInvoiceRepository
{
    Task<InvoiceDto?>             GetByIdAsync(int id);
    Task<IEnumerable<InvoiceDto>> GetAllAsync();
    Task<IEnumerable<InvoiceDto>> GetByLeaseIdAsync(int leaseId);
    /// <summary>Returns invoices for all leases belonging to tenants linked to this user.</summary>
    Task<IEnumerable<InvoiceDto>> GetByUserIdAsync(string userId);
    Task<IEnumerable<InvoiceDto>> GetByTenantIdAsync(int tenantId);
    Task<IEnumerable<InvoiceDto>> GetOverdueAsync();
    Task<int?>                    GetTenantIdForInvoiceAsync(int invoiceId);
    Task<InvoiceDto>              CreateAsync(CreateInvoiceDto dto);
    Task                          UpdateStatusAsync(int id, string status);
    Task<bool>                    ExistsAsync(int id);
    Task<string>                  GenerateInvoiceNumberAsync();
}
