using BMS.Application.DTOs.Units;
using BMS.Application.Interfaces.Repositories;
using BMS.Infrastructure.Data;
using BMS.Infrastructure.Entities;
using BMS.Infrastructure.Enums;
using Microsoft.EntityFrameworkCore;

namespace BMS.Infrastructure.Repositories;

public class UnitRepository : IUnitRepository
{
    private readonly ApplicationDbContext _context;

    public UnitRepository(ApplicationDbContext context) => _context = context;

    public async Task<UnitDto?> GetByIdAsync(int id)
    {
        var unit = await _context.Units.FindAsync(id);
        return unit is null ? null : MapToDto(unit);
    }

    public async Task<IEnumerable<UnitDto>> GetAllAsync()
    {
        var units = await _context.Units
            .OrderBy(u => u.FloorNumber)
            .ThenBy(u => u.UnitNumber)
            .ToListAsync();
        return units.Select(MapToDto);
    }

    public async Task<UnitDto> CreateAsync(CreateUnitDto dto)
    {
        var unit = new Unit
        {
            FloorNumber  = dto.FloorNumber,
            UnitNumber   = dto.UnitNumber,
            UnitType     = Enum.Parse<UnitType>(dto.UnitType, ignoreCase: true),
            AreaSqMeters = dto.AreaSqMeters,
            MonthlyRent  = dto.MonthlyRent,
            Status       = UnitStatus.Available,
            Description  = dto.Description
        };

        _context.Units.Add(unit);
        await _context.SaveChangesAsync();
        return MapToDto(unit);
    }

    public async Task UpdateAsync(int id, UpdateUnitDto dto)
    {
        var unit = await _context.Units.FindAsync(id)
                   ?? throw new KeyNotFoundException($"Unit {id} not found.");

        unit.FloorNumber  = dto.FloorNumber;
        unit.UnitNumber   = dto.UnitNumber;
        unit.UnitType     = Enum.Parse<UnitType>(dto.UnitType, ignoreCase: true);
        unit.AreaSqMeters = dto.AreaSqMeters;
        unit.MonthlyRent  = dto.MonthlyRent;
        unit.Status       = Enum.Parse<UnitStatus>(dto.Status, ignoreCase: true);
        unit.Description  = dto.Description;

        await _context.SaveChangesAsync();
    }

    public async Task DeleteAsync(int id)
    {
        var unit = await _context.Units.FindAsync(id)
                   ?? throw new KeyNotFoundException($"Unit {id} not found.");

        // Prevent deletion if any lease exists (active or historical)
        var hasLease = await _context.Leases.AnyAsync(l => l.UnitId == id);
        if (hasLease)
            throw new InvalidOperationException(
                "Cannot delete a unit that has active or historical lease records.");

        _context.Units.Remove(unit);
        await _context.SaveChangesAsync();
    }

    public Task<bool> ExistsAsync(int id) =>
        _context.Units.AnyAsync(u => u.Id == id);

    public Task<bool> IsUnitNumberTakenAsync(string unitNumber, int? excludeId = null) =>
        excludeId.HasValue
            ? _context.Units.AnyAsync(u => u.UnitNumber == unitNumber && u.Id != excludeId.Value)
            : _context.Units.AnyAsync(u => u.UnitNumber == unitNumber);

    public Task<int> CountByFloorAsync(int floorNumber, int? excludeUnitId = null) =>
        excludeUnitId.HasValue
            ? _context.Units.CountAsync(u => u.FloorNumber == floorNumber && u.Id != excludeUnitId.Value)
            : _context.Units.CountAsync(u => u.FloorNumber == floorNumber);

    // ── Mapping ─────────────────────────────────────────────────────────────

    private static UnitDto MapToDto(Unit u) => new()
    {
        Id           = u.Id,
        FloorNumber  = u.FloorNumber,
        UnitNumber   = u.UnitNumber,
        UnitType     = u.UnitType.ToString(),
        AreaSqMeters = u.AreaSqMeters,
        MonthlyRent  = u.MonthlyRent,
        Status       = u.Status.ToString(),
        Description  = u.Description
    };
}
