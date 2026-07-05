using BMS.Application.DTOs.Leases;
using BMS.Application.Interfaces.Repositories;
using BMS.Infrastructure.Data;
using BMS.Infrastructure.Entities;
using BMS.Infrastructure.Enums;
using Microsoft.EntityFrameworkCore;

namespace BMS.Infrastructure.Repositories;

public class LeaseRepository : ILeaseRepository
{
    private readonly ApplicationDbContext _context;

    public LeaseRepository(ApplicationDbContext context) => _context = context;

    public async Task<LeaseDto?> GetByIdAsync(int id)
    {
        var lease = await _context.Leases
            .Include(l => l.Unit)
            .Include(l => l.Tenant)
            .FirstOrDefaultAsync(l => l.Id == id);

        return lease is null ? null : MapToDto(lease);
    }

    public async Task<IEnumerable<LeaseDto>> GetAllAsync()
    {
        var leases = await _context.Leases
            .Include(l => l.Unit)
            .Include(l => l.Tenant)
            .OrderByDescending(l => l.StartDate)
            .ToListAsync();

        return leases.Select(MapToDto);
    }

    public async Task<IEnumerable<LeaseDto>> GetByTenantIdAsync(int tenantId)
    {
        var leases = await _context.Leases
            .Include(l => l.Unit)
            .Include(l => l.Tenant)
            .Where(l => l.TenantId == tenantId)
            .OrderByDescending(l => l.StartDate)
            .ToListAsync();

        return leases.Select(MapToDto);
    }

    public async Task<IEnumerable<LeaseDto>> GetByUnitIdAsync(int unitId)
    {
        var leases = await _context.Leases
            .Include(l => l.Unit)
            .Include(l => l.Tenant)
            .Where(l => l.UnitId == unitId)
            .OrderByDescending(l => l.StartDate)
            .ToListAsync();

        return leases.Select(MapToDto);
    }

    public async Task<LeaseDto?> GetActiveLeaseForUnitAsync(int unitId)
    {
        var lease = await _context.Leases
            .Include(l => l.Unit)
            .Include(l => l.Tenant)
            .FirstOrDefaultAsync(l => l.UnitId == unitId && l.Status == LeaseStatus.Active);

        return lease is null ? null : MapToDto(lease);
    }

    public async Task<LeaseDto> CreateAsync(CreateLeaseDto dto)
    {
        var lease = new Lease
        {
            UnitId        = dto.UnitId,
            TenantId      = dto.TenantId,
            StartDate     = dto.StartDate,
            EndDate       = dto.EndDate,
            MonthlyRent   = dto.MonthlyRent,
            DepositAmount = dto.DepositAmount,
            Status        = LeaseStatus.Active
        };

        _context.Leases.Add(lease);

        // Auto-set unit status to Occupied
        var unit = await _context.Units.FindAsync(dto.UnitId);
        if (unit is not null)
            unit.Status = UnitStatus.Occupied;

        await _context.SaveChangesAsync();

        // Reload with navigation properties for the response DTO
        return (await GetByIdAsync(lease.Id))!;
    }

    public async Task UpdateAsync(int id, UpdateLeaseDto dto)
    {
        var lease = await _context.Leases.FindAsync(id)
                    ?? throw new KeyNotFoundException($"Lease {id} not found.");

        lease.EndDate       = dto.EndDate;
        lease.MonthlyRent   = dto.MonthlyRent;
        lease.DepositAmount = dto.DepositAmount;
        lease.Status        = Enum.Parse<LeaseStatus>(dto.Status, ignoreCase: true);

        await _context.SaveChangesAsync();
    }

    public async Task TerminateAsync(int id, string reason)
    {
        var lease = await _context.Leases
            .Include(l => l.Unit)
            .FirstOrDefaultAsync(l => l.Id == id)
            ?? throw new KeyNotFoundException($"Lease {id} not found.");

        lease.Status            = LeaseStatus.Terminated;
        lease.TerminationReason = reason;

        // Auto-set unit back to Available
        if (lease.Unit is not null)
            lease.Unit.Status = UnitStatus.Available;

        await _context.SaveChangesAsync();
    }

    public Task<bool> ExistsAsync(int id) =>
        _context.Leases.AnyAsync(l => l.Id == id);

    // ── Mapping ─────────────────────────────────────────────────────────────

    private static LeaseDto MapToDto(Lease l) => new()
    {
        Id                = l.Id,
        UnitId            = l.UnitId,
        UnitNumber        = l.Unit?.UnitNumber ?? string.Empty,
        TenantId          = l.TenantId,
        TenantName        = l.Tenant?.OrganizationName ?? string.Empty,
        StartDate         = l.StartDate,
        EndDate           = l.EndDate,
        MonthlyRent       = l.MonthlyRent,
        DepositAmount     = l.DepositAmount,
        Status            = l.Status.ToString(),
        TerminationReason = l.TerminationReason
    };
}
