using BMS.Application.DTOs.Invoices;

namespace BMS.Application.Interfaces.Repositories;

public interface IInvoiceRepository
{
    Task<InvoiceDto?>             GetByIdAsync(int id);
    Task<IEnumerable<InvoiceDto>> GetAllAsync();
    Task<IEnumerable<InvoiceDto>> GetByLeaseIdAsync(int leaseId);
    Task<IEnumerable<InvoiceDto>> GetOverdueAsync();
    Task<InvoiceDto>              CreateAsync(CreateInvoiceDto dto);
    Task                          UpdateStatusAsync(int id, string status);
    Task<bool>                    ExistsAsync(int id);
    Task<string>                  GenerateInvoiceNumberAsync();
}
