using BMS.Application.Common;
using BMS.Application.DTOs.Leases;
using BMS.Application.Interfaces;
using BMS.Application.Interfaces.Repositories;

namespace BMS.Application.Services;

public class LeaseService : ILeaseService
{
    private readonly ILeaseRepository     _leaseRepository;
    private readonly IUnitRepository      _unitRepository;
    private readonly ICurrentUserService  _currentUser;

    public LeaseService(
        ILeaseRepository leaseRepository,
        IUnitRepository unitRepository,
        ICurrentUserService currentUser)
    {
        _leaseRepository = leaseRepository;
        _unitRepository  = unitRepository;
        _currentUser     = currentUser;
    }

    public async Task<IEnumerable<LeaseDto>> GetAllAsync()
    {
        if (_currentUser.IsViewer)
        {
            if (!_currentUser.TenantId.HasValue)
                return Enumerable.Empty<LeaseDto>();

            return await _leaseRepository.GetByTenantIdAsync(_currentUser.TenantId.Value);
        }

        return await _leaseRepository.GetAllAsync();
    }

    public async Task<IEnumerable<LeaseDto>> GetByTenantIdAsync(int tenantId)
    {
        if (_currentUser.IsViewer)
            DataScope.EnsureViewerTenantAccess(_currentUser, tenantId);

        return await _leaseRepository.GetByTenantIdAsync(tenantId);
    }

    public async Task<IEnumerable<LeaseDto>> GetByUnitIdAsync(int unitId)
    {
        var leases = await _leaseRepository.GetByUnitIdAsync(unitId);

        if (_currentUser.IsViewer && _currentUser.TenantId.HasValue)
            return leases.Where(l => l.TenantId == _currentUser.TenantId.Value);

        if (_currentUser.IsViewer)
            return Enumerable.Empty<LeaseDto>();

        return leases;
    }

    public async Task<LeaseDto> GetByIdAsync(int id)
    {
        var lease = await _leaseRepository.GetByIdAsync(id);
        if (lease is null)
            throw new KeyNotFoundException($"Lease {id} not found.");

        DataScope.EnsureViewerTenantAccess(_currentUser, lease.TenantId);
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
        await GetByIdAsync(id);

        if (!await _leaseRepository.ExistsAsync(id))
            throw new KeyNotFoundException($"Lease {id} not found.");

        await _leaseRepository.UpdateAsync(id, dto);
        return (await _leaseRepository.GetByIdAsync(id))!;
    }

    public async Task TerminateAsync(int id, TerminateLeaseDto dto)
    {
        await GetByIdAsync(id);

        if (!await _leaseRepository.ExistsAsync(id))
            throw new KeyNotFoundException($"Lease {id} not found.");

        await _leaseRepository.TerminateAsync(id, dto.Reason);
    }
}
