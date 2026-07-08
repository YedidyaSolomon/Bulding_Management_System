using BMS.Application.DTOs.Units;
using BMS.Application.Interfaces;
using BMS.Application.Interfaces.Repositories;

namespace BMS.Application.Services;

public class UnitService : IUnitService
{
    private const int MaxFloor        = 7;
    private const int MaxUnitsPerFloor = 3;

    private readonly IUnitRepository _unitRepository;

    public UnitService(IUnitRepository unitRepository)
    {
        _unitRepository = unitRepository;
    }

    public Task<IEnumerable<UnitDto>> GetAllAsync() =>
        _unitRepository.GetAllAsync();

    public async Task<UnitDto> GetByIdAsync(int id)
    {
        var unit = await _unitRepository.GetByIdAsync(id);
        return unit ?? throw new KeyNotFoundException($"Unit {id} not found.");
    }

    public async Task<UnitDto> CreateAsync(CreateUnitDto dto)
    {
        // ── Rule 1: floor must be 1–7 ────────────────────────────────────────
        if (dto.FloorNumber < 1 || dto.FloorNumber > MaxFloor)
            throw new InvalidOperationException(
                $"Invalid floor number '{dto.FloorNumber}'. " +
                $"This building has {MaxFloor} floors (1–{MaxFloor}).");

        // ── Rule 2: max 3 units per floor ────────────────────────────────────
        var countOnFloor = await _unitRepository.CountByFloorAsync(dto.FloorNumber);
        if (countOnFloor >= MaxUnitsPerFloor)
            throw new InvalidOperationException(
                $"Floor {dto.FloorNumber} already has the maximum of " +
                $"{MaxUnitsPerFloor} units. No more units can be added to this floor.");

        // ── Rule 3: unit number must be unique ───────────────────────────────
        if (await _unitRepository.IsUnitNumberTakenAsync(dto.UnitNumber))
            throw new InvalidOperationException($"Unit number '{dto.UnitNumber}' already exists.");

        return await _unitRepository.CreateAsync(dto);
    }

    public async Task<UnitDto> UpdateAsync(int id, UpdateUnitDto dto)
    {
        if (!await _unitRepository.ExistsAsync(id))
            throw new KeyNotFoundException($"Unit {id} not found.");

        // ── Rule 1: floor must be 1–7 ────────────────────────────────────────
        if (dto.FloorNumber < 1 || dto.FloorNumber > MaxFloor)
            throw new InvalidOperationException(
                $"Invalid floor number '{dto.FloorNumber}'. " +
                $"This building has {MaxFloor} floors (1–{MaxFloor}).");

        // ── Rule 2: max 3 units per floor (exclude the unit being moved) ─────
        var countOnFloor = await _unitRepository.CountByFloorAsync(dto.FloorNumber, excludeUnitId: id);
        if (countOnFloor >= MaxUnitsPerFloor)
            throw new InvalidOperationException(
                $"Floor {dto.FloorNumber} already has the maximum of " +
                $"{MaxUnitsPerFloor} units. Cannot move this unit to floor {dto.FloorNumber}.");

        // ── Rule 3: unit number must be unique ───────────────────────────────
        if (await _unitRepository.IsUnitNumberTakenAsync(dto.UnitNumber, excludeId: id))
            throw new InvalidOperationException($"Unit number '{dto.UnitNumber}' already exists.");

        await _unitRepository.UpdateAsync(id, dto);
        return (await _unitRepository.GetByIdAsync(id))!;
    }

    public async Task DeleteAsync(int id)
    {
        if (!await _unitRepository.ExistsAsync(id))
            throw new KeyNotFoundException($"Unit {id} not found.");

        await _unitRepository.DeleteAsync(id);
    }

    public Task<IEnumerable<UnitDto>> GetSelectableForLeaseAsync(int tenantId) =>
        _unitRepository.GetSelectableForLeaseAsync(tenantId);

    public async Task<UnitDto> ReserveAsync(int unitId, int tenantId)
    {
        if (!await _unitRepository.ExistsAsync(unitId))
            throw new KeyNotFoundException($"Unit {unitId} not found.");

        return await _unitRepository.ReserveAsync(unitId, tenantId);
    }
}
