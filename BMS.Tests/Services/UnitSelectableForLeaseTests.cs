using BMS.Application.DTOs.Units;
using BMS.Application.Interfaces.Repositories;
using BMS.Application.Services;
using Moq;
using Xunit;

namespace BMS.Tests.Services;

/// <summary>
/// Tests for the selectable-for-lease unit scoping logic.
///
///   Scenario A — Tenant A has Unit 101 reserved:
///     · Tenant A sees Unit 101 (reserved) + Unit 201 (available)
///     · Tenant B sees Unit 201 (available) only — NOT Unit 101
///
///   Scenario B — No reservations:
///     · Any tenant sees all available units
///
///   Scenario C — UnitService delegates straight to the repository:
///     · GetSelectableForLeaseAsync calls IUnitRepository.GetSelectableForLeaseAsync
///
///   Scenario D — Reserved unit has IsReservedForRequestedTenant = true:
///     · The flag is set on the reserved unit when it matches the queried tenant
///     · The flag is false for plain available units
/// </summary>
public class UnitSelectableForLeaseTests
{
    // ── Fixture data ──────────────────────────────────────────────────────────

    private const int TenantA = 10;
    private const int TenantB = 20;

    private static readonly UnitDto ReservedForTenantA = new()
    {
        Id                           = 1,
        FloorNumber                  = 1,
        UnitNumber                   = "101",
        UnitType                     = "Shop",
        AreaSqMeters                 = 50m,
        MonthlyRent                  = 5000m,
        Status                       = "Reserved",
        ReservedForTenantId          = TenantA,
        IsReservedForRequestedTenant = true,    // repo sets this for matching tenant
    };

    private static readonly UnitDto Available201 = new()
    {
        Id                           = 2,
        FloorNumber                  = 2,
        UnitNumber                   = "201",
        UnitType                     = "Office",
        AreaSqMeters                 = 80m,
        MonthlyRent                  = 8000m,
        Status                       = "Available",
        ReservedForTenantId          = null,
        IsReservedForRequestedTenant = false,
    };

    private static readonly UnitDto Available301 = new()
    {
        Id                           = 3,
        FloorNumber                  = 3,
        UnitNumber                   = "301",
        UnitType                     = "Shop",
        AreaSqMeters                 = 60m,
        MonthlyRent                  = 6000m,
        Status                       = "Available",
        ReservedForTenantId          = null,
        IsReservedForRequestedTenant = false,
    };

    // ── Scenario A: Tenant A sees their reserved unit + available units ────────

    [Fact]
    public async Task GetSelectableForLease_TenantA_SeesReservedUnitAndAvailableUnits()
    {
        var repoMock = new Mock<IUnitRepository>();
        repoMock
            .Setup(r => r.GetSelectableForLeaseAsync(TenantA))
            .ReturnsAsync(new[] { ReservedForTenantA, Available201 });

        var service = new UnitService(repoMock.Object);
        var result  = (await service.GetSelectableForLeaseAsync(TenantA)).ToList();

        Assert.Equal(2, result.Count);
        Assert.Contains(result, u => u.Id == 1 && u.IsReservedForRequestedTenant);
        Assert.Contains(result, u => u.Id == 2 && !u.IsReservedForRequestedTenant);
    }

    // ── Scenario A: Tenant B does NOT see Unit 101 (reserved for Tenant A) ────

    [Fact]
    public async Task GetSelectableForLease_TenantB_DoesNotSeeUnitReservedForTenantA()
    {
        var repoMock = new Mock<IUnitRepository>();
        // Repo correctly filters: TenantB only gets Available units
        repoMock
            .Setup(r => r.GetSelectableForLeaseAsync(TenantB))
            .ReturnsAsync(new[] { Available201 });   // Unit 101 (reserved for A) not included

        var service = new UnitService(repoMock.Object);
        var result  = (await service.GetSelectableForLeaseAsync(TenantB)).ToList();

        Assert.Single(result);
        Assert.DoesNotContain(result, u => u.ReservedForTenantId == TenantA);
        Assert.DoesNotContain(result, u => u.IsReservedForRequestedTenant);
    }

    // ── Scenario B: No reservations — all available units appear for any tenant ─

    [Fact]
    public async Task GetSelectableForLease_NoReservations_AllAvailableUnitsVisible()
    {
        var repoMock = new Mock<IUnitRepository>();
        repoMock
            .Setup(r => r.GetSelectableForLeaseAsync(It.IsAny<int>()))
            .ReturnsAsync(new[] { Available201, Available301 });

        var service = new UnitService(repoMock.Object);

        var forA = (await service.GetSelectableForLeaseAsync(TenantA)).ToList();
        var forB = (await service.GetSelectableForLeaseAsync(TenantB)).ToList();

        Assert.Equal(2, forA.Count);
        Assert.Equal(2, forB.Count);
        Assert.All(forA, u => Assert.False(u.IsReservedForRequestedTenant));
        Assert.All(forB, u => Assert.False(u.IsReservedForRequestedTenant));
    }

    // ── Scenario C: Service delegates to repository ───────────────────────────

    [Fact]
    public async Task GetSelectableForLease_DelegatesToRepository()
    {
        var repoMock = new Mock<IUnitRepository>();
        repoMock
            .Setup(r => r.GetSelectableForLeaseAsync(TenantA))
            .ReturnsAsync(Array.Empty<UnitDto>());

        var service = new UnitService(repoMock.Object);
        await service.GetSelectableForLeaseAsync(TenantA);

        repoMock.Verify(r => r.GetSelectableForLeaseAsync(TenantA), Times.Once);
    }

    // ── Scenario D: IsReservedForRequestedTenant flag is set correctly ─────────

    [Fact]
    public async Task GetSelectableForLease_ReservedUnit_HasFlagSetTrue_ForMatchingTenant()
    {
        var repoMock = new Mock<IUnitRepository>();
        repoMock
            .Setup(r => r.GetSelectableForLeaseAsync(TenantA))
            .ReturnsAsync(new[] { ReservedForTenantA, Available201 });

        var service = new UnitService(repoMock.Object);
        var result  = (await service.GetSelectableForLeaseAsync(TenantA)).ToList();

        var reserved  = result.First(u => u.Id == ReservedForTenantA.Id);
        var available = result.First(u => u.Id == Available201.Id);

        Assert.True(reserved.IsReservedForRequestedTenant,
            "The unit reserved for this tenant should have the flag set.");
        Assert.False(available.IsReservedForRequestedTenant,
            "A plain available unit should NOT have the reserved flag.");
    }

    // ── Scenario E: Empty tenant — returns empty list gracefully ──────────────

    [Fact]
    public async Task GetSelectableForLease_UnknownTenantId_ReturnsEmptyList()
    {
        var repoMock = new Mock<IUnitRepository>();
        repoMock
            .Setup(r => r.GetSelectableForLeaseAsync(It.IsAny<int>()))
            .ReturnsAsync(Array.Empty<UnitDto>());

        var service = new UnitService(repoMock.Object);
        var result  = await service.GetSelectableForLeaseAsync(999);

        Assert.Empty(result);
    }
}
