using BMS.Application.DTOs.Leases;
using BMS.Application.Interfaces;
using BMS.Application.Interfaces.Repositories;
using BMS.Application.Services;
using BMS.Tests.Helpers;
using Moq;
using Xunit;

namespace BMS.Tests.Services;

/// <summary>
/// Verifies that LeaseService correctly enforces Viewer data scoping:
///
///   GetAllAsync   — Viewer receives only their own tenant's leases.
///   GetAllAsync   — Admin/Manager receive all leases unfiltered.
///   GetByIdAsync  — Viewer accessing their own lease succeeds.
///   GetByIdAsync  — Viewer accessing a different tenant's lease throws.
///   GetByIdAsync  — Admin accessing any lease succeeds.
///   GetAllAsync   — Viewer with no linked tenant receives empty list.
/// </summary>
public class LeaseServiceScopingTests
{
    // ── Shared test data ──────────────────────────────────────────────────────

    private const int ViewerTenantId  = 10;
    private const int OtherTenantId   = 99;

    /// <summary>Two leases belonging to the Viewer's tenant.</summary>
    private static readonly IEnumerable<LeaseDto> ViewerLeases = new[]
    {
        MakeLease(id: 1, tenantId: ViewerTenantId),
        MakeLease(id: 2, tenantId: ViewerTenantId),
    };

    /// <summary>All leases in the system (Viewer's + another tenant's).</summary>
    private static readonly IEnumerable<LeaseDto> AllLeases = new[]
    {
        MakeLease(id: 1, tenantId: ViewerTenantId),
        MakeLease(id: 2, tenantId: ViewerTenantId),
        MakeLease(id: 3, tenantId: OtherTenantId),
        MakeLease(id: 4, tenantId: OtherTenantId),
    };

    // ─────────────────────────────────────────────────────────────────────────
    // GetAllAsync
    // ─────────────────────────────────────────────────────────────────────────

    [Fact]
    public async Task GetAllAsync_Viewer_ReturnsOnlyOwnTenantLeases()
    {
        // Arrange
        var (service, _) = BuildService(
            currentUser: CurrentUserFactory.Viewer(tenantId: ViewerTenantId),
            byTenantResult: ViewerLeases);

        // Act
        var result = await service.GetAllAsync();

        // Assert — all returned leases must belong to the Viewer's tenant
        Assert.NotEmpty(result);
        Assert.All(result, l => Assert.Equal(ViewerTenantId, l.TenantId));
        Assert.Equal(2, result.Count());
    }

    [Fact]
    public async Task GetAllAsync_Admin_ReturnsAllLeases()
    {
        // Arrange
        var (service, _) = BuildService(
            currentUser: CurrentUserFactory.Admin(),
            getAllResult: AllLeases);

        // Act
        var result = await service.GetAllAsync();

        // Assert — Admin sees everything
        Assert.Equal(4, result.Count());
    }

    [Fact]
    public async Task GetAllAsync_Manager_ReturnsAllLeases()
    {
        // Arrange
        var (service, _) = BuildService(
            currentUser: CurrentUserFactory.Manager(),
            getAllResult: AllLeases);

        // Act
        var result = await service.GetAllAsync();

        Assert.Equal(4, result.Count());
    }

    [Fact]
    public async Task GetAllAsync_ViewerWithNoTenant_ReturnsEmptyList()
    {
        // Arrange — viewer account exists but has no linked tenant yet
        var (service, _) = BuildService(
            currentUser: CurrentUserFactory.ViewerWithNoTenant());

        // Act
        var result = await service.GetAllAsync();

        // Assert — must return empty, not throw
        Assert.Empty(result);
    }

    // ─────────────────────────────────────────────────────────────────────────
    // GetByIdAsync — ownership enforcement
    // ─────────────────────────────────────────────────────────────────────────

    [Fact]
    public async Task GetByIdAsync_Viewer_OwnLease_ReturnsLease()
    {
        // Arrange — lease belongs to Viewer's tenant
        var ownLease = MakeLease(id: 1, tenantId: ViewerTenantId);
        var (service, _) = BuildService(
            currentUser: CurrentUserFactory.Viewer(tenantId: ViewerTenantId),
            byIdResult: ownLease);

        // Act
        var result = await service.GetByIdAsync(1);

        // Assert — should succeed without exception
        Assert.NotNull(result);
        Assert.Equal(1, result.Id);
        Assert.Equal(ViewerTenantId, result.TenantId);
    }

    [Fact]
    public async Task GetByIdAsync_Viewer_OtherTenantLease_ThrowsKeyNotFoundException()
    {
        // Arrange — lease belongs to a DIFFERENT tenant; Viewer should not see it.
        // DataScope.EnsureViewerTenantAccess throws KeyNotFoundException to mask the resource.
        var otherLease = MakeLease(id: 3, tenantId: OtherTenantId);
        var (service, _) = BuildService(
            currentUser: CurrentUserFactory.Viewer(tenantId: ViewerTenantId),
            byIdResult: otherLease);

        // Act & Assert
        await Assert.ThrowsAsync<KeyNotFoundException>(
            () => service.GetByIdAsync(3));
    }

    [Fact]
    public async Task GetByIdAsync_Admin_AnyLease_ReturnsLease()
    {
        // Arrange — Admin requests a lease belonging to any tenant
        var anyLease = MakeLease(id: 3, tenantId: OtherTenantId);
        var (service, _) = BuildService(
            currentUser: CurrentUserFactory.Admin(),
            byIdResult: anyLease);

        // Act
        var result = await service.GetByIdAsync(3);

        // Assert — Admin sees it
        Assert.Equal(3, result.Id);
    }

    [Fact]
    public async Task GetByIdAsync_Manager_AnyLease_ReturnsLease()
    {
        var anyLease = MakeLease(id: 3, tenantId: OtherTenantId);
        var (service, _) = BuildService(
            currentUser: CurrentUserFactory.Manager(),
            byIdResult: anyLease);

        var result = await service.GetByIdAsync(3);

        Assert.Equal(3, result.Id);
    }

    [Fact]
    public async Task GetByIdAsync_NonExistentLease_ThrowsKeyNotFoundException()
    {
        // Arrange — repository returns null (lease does not exist)
        var (service, _) = BuildService(
            currentUser: CurrentUserFactory.Admin(),
            byIdResult: null);   // simulates "not found" from DB

        // Act & Assert — service must surface 404
        await Assert.ThrowsAsync<KeyNotFoundException>(
            () => service.GetByIdAsync(999));
    }

    // ─────────────────────────────────────────────────────────────────────────
    // GetByTenantIdAsync — Viewer can only request their own tenantId
    // ─────────────────────────────────────────────────────────────────────────

    [Fact]
    public async Task GetByTenantIdAsync_Viewer_OwnTenantId_ReturnsLeases()
    {
        var (service, _) = BuildService(
            currentUser: CurrentUserFactory.Viewer(tenantId: ViewerTenantId),
            byTenantResult: ViewerLeases);

        var result = await service.GetByTenantIdAsync(ViewerTenantId);

        Assert.Equal(2, result.Count());
    }

    [Fact]
    public async Task GetByTenantIdAsync_Viewer_OtherTenantId_ThrowsKeyNotFoundException()
    {
        var (service, _) = BuildService(
            currentUser: CurrentUserFactory.Viewer(tenantId: ViewerTenantId));

        await Assert.ThrowsAsync<KeyNotFoundException>(
            () => service.GetByTenantIdAsync(OtherTenantId));
    }

    [Fact]
    public async Task GetByTenantIdAsync_Admin_AnyTenantId_ReturnsLeases()
    {
        var otherLeases = AllLeases.Where(l => l.TenantId == OtherTenantId);
        var (service, _) = BuildService(
            currentUser: CurrentUserFactory.Admin(),
            byTenantResult: otherLeases);

        var result = await service.GetByTenantIdAsync(OtherTenantId);

        Assert.Equal(2, result.Count());
    }

    // ─────────────────────────────────────────────────────────────────────────
    // Helpers
    // ─────────────────────────────────────────────────────────────────────────

    /// <summary>
    /// Builds a fully mocked LeaseService.
    /// Returns both the service and the underlying lease repository mock so
    /// individual tests can verify call counts if needed.
    /// </summary>
    private static (LeaseService service, Mock<ILeaseRepository> repoMock) BuildService(
        ICurrentUserService currentUser,
        IEnumerable<LeaseDto>? getAllResult    = null,
        IEnumerable<LeaseDto>? byTenantResult = null,
        LeaseDto?              byIdResult     = null)
    {
        var leaseMock = new Mock<ILeaseRepository>();
        var unitMock  = new Mock<IUnitRepository>();

        // GetAllAsync — returns all leases (used by Admin/Manager path)
        leaseMock
            .Setup(r => r.GetAllAsync())
            .ReturnsAsync(getAllResult ?? Enumerable.Empty<LeaseDto>());

        // GetByTenantIdAsync — returns leases for a specific tenant
        leaseMock
            .Setup(r => r.GetByTenantIdAsync(It.IsAny<int>()))
            .ReturnsAsync(byTenantResult ?? Enumerable.Empty<LeaseDto>());

        // GetByIdAsync — returns the configured lease or null
        leaseMock
            .Setup(r => r.GetByIdAsync(It.IsAny<int>()))
            .ReturnsAsync(byIdResult);

        var service = new LeaseService(leaseMock.Object, unitMock.Object, currentUser);
        return (service, leaseMock);
    }

    private static LeaseDto MakeLease(int id, int tenantId) => new()
    {
        Id            = id,
        TenantId      = tenantId,
        UnitId        = id * 10,
        UnitNumber    = $"U{id * 10:D3}",
        TenantName    = $"Tenant {tenantId}",
        StartDate     = DateTime.UtcNow.AddMonths(-6),
        EndDate       = DateTime.UtcNow.AddMonths(6),
        MonthlyRent   = 5000m,
        DepositAmount = 10000m,
        Status        = "Active",
    };
}
