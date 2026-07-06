using BMS.Application.DTOs.Invoices;

namespace BMS.Application.Interfaces;

public interface IInvoiceService
{
    Task<IEnumerable<InvoiceDto>> GetAllAsync();
    Task<InvoiceDto>              GetByIdAsync(int id);
    Task<IEnumerable<InvoiceDto>> GetByLeaseIdAsync(int leaseId);
    /// <summary>Returns only the invoices for leases owned by this user's tenants.</summary>
    Task<IEnumerable<InvoiceDto>> GetByUserIdAsync(string userId);
    Task<IEnumerable<InvoiceDto>> GetOverdueAsync();
    Task<InvoiceDto>              CreateAsync(CreateInvoiceDto dto);
    Task<InvoiceDto>              IssueAsync(int id);
    Task<InvoiceDto>              CancelAsync(int id);
}
