using BMS.Application.Common;
using BMS.Application.DTOs.Invoices;
using BMS.Application.Interfaces;
using BMS.Application.Interfaces.Repositories;

namespace BMS.Application.Services;

public class InvoiceService : IInvoiceService
{
    private readonly IInvoiceRepository  _invoiceRepository;
    private readonly ILeaseRepository    _leaseRepository;
    private readonly ICurrentUserService _currentUser;

    public InvoiceService(
        IInvoiceRepository invoiceRepository,
        ILeaseRepository leaseRepository,
        ICurrentUserService currentUser)
    {
        _invoiceRepository = invoiceRepository;
        _leaseRepository   = leaseRepository;
        _currentUser       = currentUser;
    }

    public async Task<IEnumerable<InvoiceDto>> GetAllAsync()
    {
        if (_currentUser.IsViewer)
        {
            if (!_currentUser.TenantId.HasValue)
                return Enumerable.Empty<InvoiceDto>();

            return await _invoiceRepository.GetByTenantIdAsync(_currentUser.TenantId.Value);
        }

        return await _invoiceRepository.GetAllAsync();
    }

    public async Task<IEnumerable<InvoiceDto>> GetByLeaseIdAsync(int leaseId)
    {
        var lease = await _leaseRepository.GetByIdAsync(leaseId);
        if (lease is null)
            throw new KeyNotFoundException($"Lease {leaseId} not found.");

        DataScope.EnsureViewerTenantAccess(_currentUser, lease.TenantId);
        return await _invoiceRepository.GetByLeaseIdAsync(leaseId);
    }

    public async Task<IEnumerable<InvoiceDto>> GetByUserIdAsync(string userId)
    {
        var resolvedUserId = DataScope.ResolveUserId(_currentUser, userId);
        return await _invoiceRepository.GetByUserIdAsync(resolvedUserId);
    }

    public async Task<IEnumerable<InvoiceDto>> GetOverdueAsync()
    {
        var overdue = (await _invoiceRepository.GetOverdueAsync()).ToList();

        if (!_currentUser.IsViewer)
            return overdue;

        if (!_currentUser.TenantId.HasValue)
            return Enumerable.Empty<InvoiceDto>();

        var tenantInvoiceIds = (await _invoiceRepository.GetByTenantIdAsync(_currentUser.TenantId.Value))
            .Select(i => i.Id)
            .ToHashSet();

        return overdue.Where(i => tenantInvoiceIds.Contains(i.Id));
    }

    public async Task<InvoiceDto> GetByIdAsync(int id)
    {
        var invoice = await _invoiceRepository.GetByIdAsync(id);
        if (invoice is null)
            throw new KeyNotFoundException($"Invoice {id} not found.");

        var tenantId = await _invoiceRepository.GetTenantIdForInvoiceAsync(id);
        if (tenantId.HasValue)
            DataScope.EnsureViewerTenantAccess(_currentUser, tenantId.Value);

        return invoice;
    }

    public async Task<InvoiceDto> CreateAsync(CreateInvoiceDto dto)
    {
        if (!await _leaseRepository.ExistsAsync(dto.LeaseId))
            throw new KeyNotFoundException($"Lease {dto.LeaseId} not found.");

        return await _invoiceRepository.CreateAsync(dto);
    }

    public async Task<InvoiceDto> IssueAsync(int id)
    {
        await GetByIdAsync(id);

        if (!await _invoiceRepository.ExistsAsync(id))
            throw new KeyNotFoundException($"Invoice {id} not found.");

        await _invoiceRepository.UpdateStatusAsync(id, "Issued");
        return (await _invoiceRepository.GetByIdAsync(id))!;
    }

    public async Task<InvoiceDto> CancelAsync(int id)
    {
        await GetByIdAsync(id);

        if (!await _invoiceRepository.ExistsAsync(id))
            throw new KeyNotFoundException($"Invoice {id} not found.");

        await _invoiceRepository.UpdateStatusAsync(id, "Cancelled");
        return (await _invoiceRepository.GetByIdAsync(id))!;
    }
}
