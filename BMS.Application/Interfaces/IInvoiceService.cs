using BMS.Application.DTOs.Invoices;

namespace BMS.Application.Interfaces;

public interface IInvoiceService
{
    Task<IEnumerable<InvoiceDto>> GetAllAsync();
    Task<InvoiceDto>              GetByIdAsync(int id);
    Task<IEnumerable<InvoiceDto>> GetByLeaseIdAsync(int leaseId);
    Task<IEnumerable<InvoiceDto>> GetOverdueAsync();
    Task<InvoiceDto>              CreateAsync(CreateInvoiceDto dto);
    Task<InvoiceDto>              IssueAsync(int id);
    Task<InvoiceDto>              CancelAsync(int id);
}
