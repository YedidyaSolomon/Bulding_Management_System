using BMS.Application.DTOs.Leases;
using BMS.Infrastructure.Data;
using BMS.Infrastructure.Entities;
using BMS.Infrastructure.Enums;
using BMS.Infrastructure.Repositories;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace BMS.Tests.Repositories;

/// <summary>
/// Repository-level integration tests that exercise the reservation and
/// lease-creation logic against an EF Core in-memory database.
///
/// Scenarios covered:
///   UnitRepository.GetSelectableForLeaseAsync
///     — Reserved-for-TenantA unit appears for TenantA, NOT for TenantB.
///     — Available unit appears regardless of which tenant queries.
///     — IsReservedForRequestedTenant flag is set only for the matching tenant.
///
///   UnitRepository.ReserveAsync
///     — Available unit transitions to Reserved and sets ReservedForTenantId.
///     — Occupied unit throws InvalidOperationException.
///     — UnderMaintenance unit throws InvalidOperationException.
///
///   LeaseRepository.CreateAsync
///     — Transitions unit status from Reserved → Occupied.
///     — Clears ReservedForTenantId after lease is created.
///     — Available unit also becomes Occupied when leased.
/// </summary>
public class UnitLeaseReservationRepositoryTests : IDisposable
{
    private readonly ApplicationDbContext _ctx;
    private readonly UnitRepository       _unitRepo;
    private readonly LeaseRepository      _leaseRepo;

    public UnitLeaseReservationRepositoryTests()
    {
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())   // isolated per test
            .Options;

        _ctx      = new ApplicationDbContext(options);
        _unitRepo = new UnitRepository(_ctx);
        _leaseRepo = new LeaseRepository(_ctx);
    }

    public void Dispose() => _ctx.Dispose();

    // ── Seed helpers ─────────────────────────────────────────────────────────

    private const int TenantA = 10;
    private const int TenantB = 20;

    private Unit SeedUnit(
        int         id,
        UnitStatus  status              = UnitStatus.Available,
        int?        reservedForTenantId = null)
    {
        var unit = new Unit
        {
            Id                  = id,
            FloorNumber         = 1,
            UnitNumber          = $"U{id:D3}",
            UnitType            = UnitType.Office,
            AreaSqMeters        = 50m,
            MonthlyRent         = 3000m,
            Status              = status,
            ReservedForTenantId = reservedForTenantId,
        };
        _ctx.Units.Add(unit);

        // Tenant rows are needed only when ReservedForTenantId is set
        if (reservedForTenantId.HasValue &&
            !_ctx.Tenants.Any(t => t.Id == reservedForTenantId.Value))
        {
            _ctx.Tenants.Add(new Tenant
            {
                Id               = reservedForTenantId.Value,
                OrganizationName = $"Tenant {reservedForTenantId.Value}",
                TIN              = $"TIN-{reservedForTenantId.Value}",
                Phone            = "000",
                ContactPersonName = "Test",
                ContactEmail     = $"test{reservedForTenantId.Value}@test.com",
            });
        }

        _ctx.SaveChanges();
        return unit;
    }

    private Tenant SeedTenant(int id)
    {
        if (_ctx.Tenants.Any(t => t.Id == id))
            return _ctx.Tenants.Find(id)!;

        var tenant = new Tenant
        {
            Id               = id,
            OrganizationName = $"Tenant {id}",
            TIN              = $"TIN-{id}",
            Phone            = "000",
            ContactPersonName = "Test",
            ContactEmail     = $"test{id}@test.com",
        };
        _ctx.Tenants.Add(tenant);
        _ctx.SaveChanges();
        return tenant;
    }

    // ── GetSelectableForLeaseAsync ────────────────────────────────────────────

    [Fact]
    public async Task GetSelectableForLease_TenantA_IncludesUnitReservedForTenantA()
    {
        SeedUnit(id: 1, UnitStatus.Available);
        SeedUnit(id: 2, UnitStatus.Reserved, reservedForTenantId: TenantA);

        var result = (await _unitRepo.GetSelectableForLeaseAsync(TenantA)).ToList();

        Assert.Equal(2, result.Count);
        Assert.Contains(result, u => u.Id == 2 && u.IsReservedForRequestedTenant);
    }

    [Fact]
    public async Task GetSelectableForLease_TenantB_ExcludesUnitReservedForTenantA()
    {
        SeedUnit(id: 1, UnitStatus.Available);
        SeedUnit(id: 2, UnitStatus.Reserved, reservedForTenantId: TenantA);

        var result = (await _unitRepo.GetSelectableForLeaseAsync(TenantB)).ToList();

        // Only the available unit; the one reserved for TenantA must not appear
        Assert.Single(result);
        Assert.Equal(1, result[0].Id);
        Assert.DoesNotContain(result, u => u.Id == 2);
    }

    [Fact]
    public async Task GetSelectableForLease_AvailableUnit_AppearsForBothTenants()
    {
        SeedUnit(id: 1, UnitStatus.Available);

        var forA = (await _unitRepo.GetSelectableForLeaseAsync(TenantA)).ToList();
        var forB = (await _unitRepo.GetSelectableForLeaseAsync(TenantB)).ToList();

        Assert.Contains(forA, u => u.Id == 1);
        Assert.Contains(forB, u => u.Id == 1);
    }

    [Fact]
    public async Task GetSelectableForLease_IsReservedFlag_TrueOnlyForMatchingTenant()
    {
        SeedUnit(id: 1, UnitStatus.Available);
        SeedUnit(id: 2, UnitStatus.Reserved, reservedForTenantId: TenantA);

        var result = (await _unitRepo.GetSelectableForLeaseAsync(TenantA)).ToList();

        var available = result.Single(u => u.Id == 1);
        var reserved  = result.Single(u => u.Id == 2);

        Assert.False(available.IsReservedForRequestedTenant);
        Assert.True(reserved.IsReservedForRequestedTenant);
    }

    [Fact]
    public async Task GetSelectableForLease_OccupiedUnit_NeverAppears()
    {
        SeedTenant(TenantA);
        SeedUnit(id: 1, UnitStatus.Occupied);

        var result = (await _unitRepo.GetSelectableForLeaseAsync(TenantA)).ToList();

        Assert.Empty(result);
    }

    // ── UnitRepository.ReserveAsync ───────────────────────────────────────────

    [Fact]
    public async Task ReserveAsync_AvailableUnit_TransitionsToReservedAndSetsFK()
    {
        SeedTenant(TenantA);
        SeedUnit(id: 1, UnitStatus.Available);

        var dto = await _unitRepo.ReserveAsync(unitId: 1, tenantId: TenantA);

        Assert.Equal("Reserved", dto.Status);
        Assert.Equal(TenantA, dto.ReservedForTenantId);

        // Verify persisted
        var persisted = await _ctx.Units.FindAsync(1);
        Assert.Equal(UnitStatus.Reserved, persisted!.Status);
        Assert.Equal(TenantA, persisted.ReservedForTenantId);
    }

    [Fact]
    public async Task ReserveAsync_AlreadyReservedUnit_CanBeReReservedForDifferentTenant()
    {
        // Reserving a unit that was reserved for TenantA, now for TenantB
        SeedTenant(TenantA);
        SeedTenant(TenantB);
        SeedUnit(id: 1, UnitStatus.Reserved, reservedForTenantId: TenantA);

        var dto = await _unitRepo.ReserveAsync(unitId: 1, tenantId: TenantB);

        Assert.Equal("Reserved", dto.Status);
        Assert.Equal(TenantB, dto.ReservedForTenantId);
    }

    [Fact]
    public async Task ReserveAsync_OccupiedUnit_ThrowsInvalidOperation()
    {
        SeedTenant(TenantA);
        SeedUnit(id: 1, UnitStatus.Occupied);

        await Assert.ThrowsAsync<InvalidOperationException>(
            () => _unitRepo.ReserveAsync(unitId: 1, tenantId: TenantA));
    }

    [Fact]
    public async Task ReserveAsync_UnderMaintenanceUnit_ThrowsInvalidOperation()
    {
        SeedTenant(TenantA);
        SeedUnit(id: 1, UnitStatus.UnderMaintenance);

        await Assert.ThrowsAsync<InvalidOperationException>(
            () => _unitRepo.ReserveAsync(unitId: 1, tenantId: TenantA));
    }

    // ── LeaseRepository.CreateAsync — Reserved → Occupied + clear FK ─────────

    [Fact]
    public async Task CreateLease_ReservedUnit_TransitionsToOccupiedAndClearsReservation()
    {
        // Arrange: unit reserved for TenantA
        SeedTenant(TenantA);
        SeedUnit(id: 1, UnitStatus.Reserved, reservedForTenantId: TenantA);

        var dto = new CreateLeaseDto
        {
            UnitId        = 1,
            TenantId      = TenantA,
            StartDate     = DateTime.UtcNow,
            EndDate       = DateTime.UtcNow.AddYears(1),
            MonthlyRent   = 3000m,
            DepositAmount = 6000m,
        };

        // Act
        var lease = await _leaseRepo.CreateAsync(dto);

        // Assert: lease created successfully
        Assert.Equal(1, lease.UnitId);
        Assert.Equal(TenantA, lease.TenantId);

        // Assert: unit transitioned to Occupied and reservation cleared
        var unit = await _ctx.Units.FindAsync(1);
        Assert.Equal(UnitStatus.Occupied, unit!.Status);
        Assert.Null(unit.ReservedForTenantId);
    }

    [Fact]
    public async Task CreateLease_AvailableUnit_TransitionsToOccupied()
    {
        // Arrange: plain available unit (no reservation)
        SeedTenant(TenantB);
        SeedUnit(id: 2, UnitStatus.Available);

        var dto = new CreateLeaseDto
        {
            UnitId        = 2,
            TenantId      = TenantB,
            StartDate     = DateTime.UtcNow,
            EndDate       = DateTime.UtcNow.AddYears(1),
            MonthlyRent   = 4000m,
            DepositAmount = 8000m,
        };

        await _leaseRepo.CreateAsync(dto);

        var unit = await _ctx.Units.FindAsync(2);
        Assert.Equal(UnitStatus.Occupied, unit!.Status);
        Assert.Null(unit.ReservedForTenantId);   // was never set, stays null
    }

    [Fact]
    public async Task TerminateLease_ResetsUnitToAvailableAndClearsReservation()
    {
        // Arrange: create a lease for an available unit, then terminate it
        SeedTenant(TenantA);
        SeedUnit(id: 3, UnitStatus.Available);

        var createDto = new CreateLeaseDto
        {
            UnitId        = 3,
            TenantId      = TenantA,
            StartDate     = DateTime.UtcNow.AddDays(-30),
            EndDate       = DateTime.UtcNow.AddMonths(11),
            MonthlyRent   = 2000m,
            DepositAmount = 4000m,
        };

        var lease = await _leaseRepo.CreateAsync(createDto);

        // Act: terminate the lease
        await _leaseRepo.TerminateAsync(lease.Id, "Test termination");

        // Assert: unit back to Available with no reservation
        var unit = await _ctx.Units.FindAsync(3);
        Assert.Equal(UnitStatus.Available, unit!.Status);
        Assert.Null(unit.ReservedForTenantId);
    }
}
