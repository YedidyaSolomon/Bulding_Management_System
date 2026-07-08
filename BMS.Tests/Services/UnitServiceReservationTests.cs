using BMS.Application.DTOs.Units;
using BMS.Application.Interfaces.Repositories;
using BMS.Application.Services;
using Moq;
using Xunit;

namespace BMS.Tests.Services;

/// <summary>
/// Tests the reservation-aware unit selection and reservation logic added to
/// UnitService.
///
///   GetSelectableForLease — Reserved-for-TenantA unit appears when TenantA queries,
///                           but NOT when TenantB queries.
///   GetSelectableForLease — Available unit appears regardless of which tenant queries.
///   GetSelectableForLease — IsReservedForRequestedTenant is true only for the
///                           unit reserved for the queried tenant.
///   ReserveAsync          — Delegates to the repository for a valid unit.
///   ReserveAsync          — Throws KeyNotFoundException when the unit does not exist.
/// </summary>
public class UnitServiceReservationTests
{
    private const int TenantA = 10;
    private const int TenantB = 20;
    private const int UnitAvailable = 1;
    private const int UnitReservedForA = 2;

    // ── GetSelectableForLease ─────────────────────────────────────────────────

    [Fact]
    public async Task GetSelectableForLease_TenantA_IncludesUnitReservedForTenantA()
    {
        // Arrange: repo returns the reserved unit + an available unit for TenantA
        var expectedUnits = new[]
        {
            MakeUnit(UnitReservedForA, status: "Reserved", isReservedForRequested: true),
            MakeUnit(UnitAvailable,    status: "Available"),
        };

        var (service, _) = BuildService(selectableResult: expectedUnits);

        // Act
        var result = (await service.GetSelectableForLeaseAsync(TenantA)).ToList();

        // Assert: the reserved unit IS in the result
        Assert.Contains(result, u => u.Id == UnitReservedForA && u.IsReservedForRequestedTenant);
    }

    [Fact]
    public async Task GetSelectableForLease_TenantB_DoesNotIncludeUnitReservedForTenantA()
    {
        // Arrange: repo returns only Available units for TenantB (the reserved unit
        // for TenantA is filtered out by the repository query)
        var expectedUnits = new[]
        {
            MakeUnit(UnitAvailable, status: "Available"),
        };

        var (service, _) = BuildService(selectableResult: expectedUnits);

        // Act
        var result = (await service.GetSelectableForLeaseAsync(TenantB)).ToList();

        // Assert: the unit reserved for TenantA must NOT appear
        Assert.DoesNotContain(result, u => u.Id == UnitReservedForA);
    }

    [Fact]
    public async Task GetSelectableForLease_AvailableUnit_AppearsForAnyTenant()
    {
        // Arrange: always return the available unit regardless of which tenant queries
        var forTenantA = new[] { MakeUnit(UnitAvailable, status: "Available") };
        var forTenantB = new[] { MakeUnit(UnitAvailable, status: "Available") };

        // Two separate calls — one per tenant
        var unitMockA = new Mock<IUnitRepository>();
        unitMockA.Setup(r => r.GetSelectableForLeaseAsync(TenantA)).ReturnsAsync(forTenantA);
        unitMockA.Setup(r => r.ExistsAsync(It.IsAny<int>())).ReturnsAsync(true);
        var serviceA = new UnitService(unitMockA.Object);

        var unitMockB = new Mock<IUnitRepository>();
        unitMockB.Setup(r => r.GetSelectableForLeaseAsync(TenantB)).ReturnsAsync(forTenantB);
        unitMockB.Setup(r => r.ExistsAsync(It.IsAny<int>())).ReturnsAsync(true);
        var serviceB = new UnitService(unitMockB.Object);

        // Act
        var resultA = (await serviceA.GetSelectableForLeaseAsync(TenantA)).ToList();
        var resultB = (await serviceB.GetSelectableForLeaseAsync(TenantB)).ToList();

        // Assert: the available unit is present in both results
        Assert.Contains(resultA, u => u.Id == UnitAvailable);
        Assert.Contains(resultB, u => u.Id == UnitAvailable);
    }

    [Fact]
    public async Task GetSelectableForLease_ReservedForTenantA_IsReservedFlagTrueOnlyForTenantA()
    {
        // For TenantA: the reserved unit has IsReservedForRequestedTenant = true
        var forTenantA = new[]
        {
            MakeUnit(UnitReservedForA, status: "Reserved", isReservedForRequested: true),
            MakeUnit(UnitAvailable,    status: "Available"),
        };

        var (service, _) = BuildService(selectableResult: forTenantA);

        var result = (await service.GetSelectableForLeaseAsync(TenantA)).ToList();

        var reservedUnit = result.Single(u => u.Id == UnitReservedForA);
        Assert.True(reservedUnit.IsReservedForRequestedTenant);

        var availableUnit = result.Single(u => u.Id == UnitAvailable);
        Assert.False(availableUnit.IsReservedForRequestedTenant);
    }

    // ── ReserveAsync ──────────────────────────────────────────────────────────

    [Fact]
    public async Task ReserveAsync_ValidUnit_DelegatesToRepository()
    {
        var reservedDto = MakeUnit(UnitReservedForA, status: "Reserved", isReservedForRequested: true);

        var unitMock = new Mock<IUnitRepository>();
        unitMock.Setup(r => r.ExistsAsync(UnitReservedForA)).ReturnsAsync(true);
        unitMock.Setup(r => r.ReserveAsync(UnitReservedForA, TenantA)).ReturnsAsync(reservedDto);
        var service = new UnitService(unitMock.Object);

        var result = await service.ReserveAsync(UnitReservedForA, TenantA);

        unitMock.Verify(r => r.ReserveAsync(UnitReservedForA, TenantA), Times.Once);
        Assert.Equal(UnitReservedForA, result.Id);
        Assert.Equal("Reserved", result.Status);
    }

    [Fact]
    public async Task ReserveAsync_NonExistentUnit_ThrowsKeyNotFoundException()
    {
        var unitMock = new Mock<IUnitRepository>();
        unitMock.Setup(r => r.ExistsAsync(It.IsAny<int>())).ReturnsAsync(false);
        var service = new UnitService(unitMock.Object);

        await Assert.ThrowsAsync<KeyNotFoundException>(
            () => service.ReserveAsync(unitId: 999, tenantId: TenantA));
    }

    // ── Helpers ───────────────────────────────────────────────────────────────

    private static (UnitService service, Mock<IUnitRepository> repoMock) BuildService(
        IEnumerable<UnitDto>? selectableResult = null)
    {
        var unitMock = new Mock<IUnitRepository>();

        unitMock
            .Setup(r => r.GetSelectableForLeaseAsync(It.IsAny<int>()))
            .ReturnsAsync(selectableResult ?? Enumerable.Empty<UnitDto>());

        unitMock.Setup(r => r.ExistsAsync(It.IsAny<int>())).ReturnsAsync(true);

        return (new UnitService(unitMock.Object), unitMock);
    }

    private static UnitDto MakeUnit(
        int    id,
        string status                  = "Available",
        bool   isReservedForRequested  = false) => new()
    {
        Id                           = id,
        FloorNumber                  = 1,
        UnitNumber                   = $"U{id:D3}",
        UnitType                     = "Office",
        AreaSqMeters                 = 50m,
        MonthlyRent                  = 3000m,
        Status                       = status,
        Description                  = null,
        ReservedForTenantId          = isReservedForRequested ? (int?)10 : null,
        IsReservedForRequestedTenant = isReservedForRequested,
    };
}
