using BMS.Application.Common;
using BMS.Application.DTOs.Leases;
using BMS.Application.Interfaces;
using BMS.Application.Interfaces.Repositories;

namespace BMS.Application.Services;

public class LeaseService : ILeaseService
{
    private readonly ILeaseRepository        _leaseRepository;
    private readonly IUnitRepository         _unitRepository;
    private readonly ICurrentUserService     _currentUser;
    private readonly ITenantOwnershipResolver _ownershipResolver;

    public LeaseService(
        ILeaseRepository        leaseRepository,
        IUnitRepository         unitRepository,
        ICurrentUserService     currentUser,
        ITenantOwnershipResolver ownershipResolver)
    {
        _leaseRepository   = leaseRepository;
        _unitRepository    = unitRepository;
        _currentUser       = currentUser;
        _ownershipResolver = ownershipResolver;
    }

    public async Task<IEnumerable<LeaseDto>> GetAllAsync()
    {
        var ownedIds = await _ownershipResolver.GetOwnedTenantIdsAsync();

        if (ownedIds is null)
            return await _leaseRepository.GetAllAsync();

        if (ownedIds.Count == 0)
            return Enumerable.Empty<LeaseDto>();

        // Viewer with one or more tenants — union all leases across owned tenants
        var tasks  = ownedIds.Select(tid => _leaseRepository.GetByTenantIdAsync(tid));
        var results = await Task.WhenAll(tasks);
        return results.SelectMany(x => x);
    }

    public async Task<IEnumerable<LeaseDto>> GetByTenantIdAsync(int tenantId)
    {
        var ownedIds = await _ownershipResolver.GetOwnedTenantIdsAsync();
        DataScope.EnsureViewerOwnedTenantAccess(ownedIds, tenantId);
        return await _leaseRepository.GetByTenantIdAsync(tenantId);
    }

    public async Task<IEnumerable<LeaseDto>> GetByUnitIdAsync(int unitId)
    {
        var leases   = await _leaseRepository.GetByUnitIdAsync(unitId);
        var ownedIds = await _ownershipResolver.GetOwnedTenantIdsAsync();

        if (ownedIds is null)
            return leases;

        return leases.Where(l => ownedIds.Contains(l.TenantId));
    }

    public async Task<LeaseDto> GetByIdAsync(int id)
    {
        var lease = await _leaseRepository.GetByIdAsync(id);
        if (lease is null)
            throw new KeyNotFoundException($"Lease {id} not found.");

        var ownedIds = await _ownershipResolver.GetOwnedTenantIdsAsync();
        DataScope.EnsureViewerOwnedTenantAccess(ownedIds, lease.TenantId);

        return lease;
    }

    public async Task<LeaseDto> CreateAsync(CreateLeaseDto dto)
    {
        if (!await _unitRepository.ExistsAsync(dto.UnitId))
            throw new KeyNotFoundException($"Unit {dto.UnitId} not found.");

        var activeLease = await _leaseRepository.GetActiveLeaseForUnitAsync(dto.UnitId);
        if (activeLease is not null)
            throw new InvalidOperationException($"Unit {dto.UnitId} already has an active lease.");

        if (dto.StartDate >= dto.EndDate)
            throw new ArgumentException("Start date must be before end date.");

        return await _leaseRepository.CreateAsync(dto);
    }

    public async Task<LeaseDto> UpdateAsync(int id, UpdateLeaseDto dto)
    {
        await GetByIdAsync(id); // enforces scoping for Viewer

        if (!await _leaseRepository.ExistsAsync(id))
            throw new KeyNotFoundException($"Lease {id} not found.");

        await _leaseRepository.UpdateAsync(id, dto);
        return (await _leaseRepository.GetByIdAsync(id))!;
    }

    public async Task TerminateAsync(int id, TerminateLeaseDto dto)
    {
        await GetByIdAsync(id); // enforces scoping for Viewer

        if (!await _leaseRepository.ExistsAsync(id))
            throw new KeyNotFoundException($"Lease {id} not found.");

        await _leaseRepository.TerminateAsync(id, dto.Reason);
    }
}
