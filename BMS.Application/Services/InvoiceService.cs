using BMS.Application.Common;
using BMS.Application.DTOs.Invoices;
using BMS.Application.Interfaces;
using BMS.Application.Interfaces.Repositories;

namespace BMS.Application.Services;

public class InvoiceService : IInvoiceService
{
    private readonly IInvoiceRepository      _invoiceRepository;
    private readonly ILeaseRepository        _leaseRepository;
    private readonly ICurrentUserService     _currentUser;
    private readonly ITenantOwnershipResolver _ownershipResolver;

    public InvoiceService(
        IInvoiceRepository      invoiceRepository,
        ILeaseRepository        leaseRepository,
        ICurrentUserService     currentUser,
        ITenantOwnershipResolver ownershipResolver)
    {
        _invoiceRepository = invoiceRepository;
        _leaseRepository   = leaseRepository;
        _currentUser       = currentUser;
        _ownershipResolver = ownershipResolver;
    }

    public async Task<IEnumerable<InvoiceDto>> GetAllAsync()
    {
        var ownedIds = await _ownershipResolver.GetOwnedTenantIdsAsync();

        if (ownedIds is null)
            return await _invoiceRepository.GetAllAsync();

        if (ownedIds.Count == 0)
            return Enumerable.Empty<InvoiceDto>();

        var tasks   = ownedIds.Select(tid => _invoiceRepository.GetByTenantIdAsync(tid));
        var results = await Task.WhenAll(tasks);
        return results.SelectMany(x => x);
    }

    public async Task<IEnumerable<InvoiceDto>> GetByLeaseIdAsync(int leaseId)
    {
        var lease = await _leaseRepository.GetByIdAsync(leaseId);
        if (lease is null)
            throw new KeyNotFoundException($"Lease {leaseId} not found.");

        var ownedIds = await _ownershipResolver.GetOwnedTenantIdsAsync();
        DataScope.EnsureViewerOwnedTenantAccess(ownedIds, lease.TenantId);

        return await _invoiceRepository.GetByLeaseIdAsync(leaseId);
    }

    public async Task<IEnumerable<InvoiceDto>> GetByUserIdAsync(string userId)
    {
        var resolvedUserId = DataScope.ResolveUserId(_currentUser, userId);
        return await _invoiceRepository.GetByUserIdAsync(resolvedUserId);
    }

    public async Task<IEnumerable<InvoiceDto>> GetOverdueAsync()
    {
        var overdue  = (await _invoiceRepository.GetOverdueAsync()).ToList();
        var ownedIds = await _ownershipResolver.GetOwnedTenantIdsAsync();

        if (ownedIds is null)
            return overdue;

        if (ownedIds.Count == 0)
            return Enumerable.Empty<InvoiceDto>();

        // Collect invoice IDs belonging to the viewer's owned tenants
        var tasks           = ownedIds.Select(tid => _invoiceRepository.GetByTenantIdAsync(tid));
        var results         = await Task.WhenAll(tasks);
        var ownedInvoiceIds = results.SelectMany(x => x).Select(i => i.Id).ToHashSet();

        return overdue.Where(i => ownedInvoiceIds.Contains(i.Id));
    }

    public async Task<InvoiceDto> GetByIdAsync(int id)
    {
        var invoice = await _invoiceRepository.GetByIdAsync(id);
        if (invoice is null)
            throw new KeyNotFoundException($"Invoice {id} not found.");

        var tenantId = await _invoiceRepository.GetTenantIdForInvoiceAsync(id);
        if (tenantId.HasValue)
        {
            var ownedIds = await _ownershipResolver.GetOwnedTenantIdsAsync();
            DataScope.EnsureViewerOwnedTenantAccess(ownedIds, tenantId.Value);
        }

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
        await GetByIdAsync(id); // enforces scoping

        if (!await _invoiceRepository.ExistsAsync(id))
            throw new KeyNotFoundException($"Invoice {id} not found.");

        await _invoiceRepository.UpdateStatusAsync(id, "Issued");
        return (await _invoiceRepository.GetByIdAsync(id))!;
    }

    public async Task<InvoiceDto> CancelAsync(int id)
    {
        await GetByIdAsync(id); // enforces scoping

        if (!await _invoiceRepository.ExistsAsync(id))
            throw new KeyNotFoundException($"Invoice {id} not found.");

        await _invoiceRepository.UpdateStatusAsync(id, "Cancelled");
        return (await _invoiceRepository.GetByIdAsync(id))!;
    }
}
