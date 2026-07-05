using BMS.Application.DTOs.Invoices;
using BMS.Application.Interfaces;
using BMS.Application.Interfaces.Repositories;

namespace BMS.Application.Services;

public class InvoiceService : IInvoiceService
{
    private readonly IInvoiceRepository _invoiceRepository;
    private readonly ILeaseRepository   _leaseRepository;

    public InvoiceService(IInvoiceRepository invoiceRepository, ILeaseRepository leaseRepository)
    {
        _invoiceRepository = invoiceRepository;
        _leaseRepository   = leaseRepository;
    }

    public Task<IEnumerable<InvoiceDto>> GetAllAsync()                  => _invoiceRepository.GetAllAsync();
    public Task<IEnumerable<InvoiceDto>> GetByLeaseIdAsync(int leaseId) => _invoiceRepository.GetByLeaseIdAsync(leaseId);
    public Task<IEnumerable<InvoiceDto>> GetOverdueAsync()              => _invoiceRepository.GetOverdueAsync();

    public async Task<InvoiceDto> GetByIdAsync(int id)
    {
        var invoice = await _invoiceRepository.GetByIdAsync(id);
        return invoice ?? throw new KeyNotFoundException($"Invoice {id} not found.");
    }

    public async Task<InvoiceDto> CreateAsync(CreateInvoiceDto dto)
    {
        if (!await _leaseRepository.ExistsAsync(dto.LeaseId))
            throw new KeyNotFoundException($"Lease {dto.LeaseId} not found.");

        return await _invoiceRepository.CreateAsync(dto);
    }

    public async Task<InvoiceDto> IssueAsync(int id)
    {
        if (!await _invoiceRepository.ExistsAsync(id))
            throw new KeyNotFoundException($"Invoice {id} not found.");

        await _invoiceRepository.UpdateStatusAsync(id, "Issued");
        return (await _invoiceRepository.GetByIdAsync(id))!;
    }

    public async Task<InvoiceDto> CancelAsync(int id)
    {
        if (!await _invoiceRepository.ExistsAsync(id))
            throw new KeyNotFoundException($"Invoice {id} not found.");

        await _invoiceRepository.UpdateStatusAsync(id, "Cancelled");
        return (await _invoiceRepository.GetByIdAsync(id))!;
    }
}
