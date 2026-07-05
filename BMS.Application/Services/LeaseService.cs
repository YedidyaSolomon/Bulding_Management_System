using BMS.Application.DTOs.Leases;
using BMS.Application.Interfaces;
using BMS.Application.Interfaces.Repositories;

namespace BMS.Application.Services;

public class LeaseService : ILeaseService
{
    private readonly ILeaseRepository _leaseRepository;
    private readonly IUnitRepository  _unitRepository;

    public LeaseService(ILeaseRepository leaseRepository, IUnitRepository unitRepository)
    {
        _leaseRepository = leaseRepository;
        _unitRepository  = unitRepository;
    }

    public Task<IEnumerable<LeaseDto>> GetAllAsync()                    => _leaseRepository.GetAllAsync();
    public Task<IEnumerable<LeaseDto>> GetByTenantIdAsync(int tenantId) => _leaseRepository.GetByTenantIdAsync(tenantId);
    public Task<IEnumerable<LeaseDto>> GetByUnitIdAsync(int unitId)     => _leaseRepository.GetByUnitIdAsync(unitId);

    public async Task<LeaseDto> GetByIdAsync(int id)
    {
        var lease = await _leaseRepository.GetByIdAsync(id);
        return lease ?? throw new KeyNotFoundException($"Lease {id} not found.");
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
        if (!await _leaseRepository.ExistsAsync(id))
            throw new KeyNotFoundException($"Lease {id} not found.");

        await _leaseRepository.UpdateAsync(id, dto);
        return (await _leaseRepository.GetByIdAsync(id))!;
    }

    public async Task TerminateAsync(int id, TerminateLeaseDto dto)
    {
        if (!await _leaseRepository.ExistsAsync(id))
            throw new KeyNotFoundException($"Lease {id} not found.");

        await _leaseRepository.TerminateAsync(id, dto.Reason);
    }
}
