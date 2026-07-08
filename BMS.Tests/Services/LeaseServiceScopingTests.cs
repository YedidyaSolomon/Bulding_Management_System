using BMS.Application.DTOs.Leases;
using BMS.Application.Exceptions;
using BMS.Application.Interfaces;
using BMS.Application.Interfaces.Repositories;
using BMS.Application.Services;
using BMS.Tests.Helpers;
using Moq;
using Xunit;

namespace BMS.Tests.Services;

/// <summary>
/// Verifies that LeaseService correctly enforces Viewer data scoping
/// using the new ITenantOwnershipResolver architecture.
///
///   GetAllAsync   — Viewer with one tenant sees only that tenant's leases.
///   GetAllAsync   — Viewer with TWO tenants sees combined leases from both.
///   GetAllAsync   — Viewer with no tenants receives empty list.
///   GetAllAsync   — Admin/Manager receives all leases.
///   GetByIdAsync  — Viewer accessing own tenant's lease succeeds.
///   GetByIdAsync  — Viewer accessing different tenant's lease throws 403.
///   GetByIdAsync  — Admin accessing any lease succeeds.
///   GetByTenantId — Viewer requesting own tenantId succeeds.
///   GetByTenantId — Viewer requesting other tenantId throws 403.
///   GetByTenantId — Admin requesting any tenantId succeeds.
/// </summary>
public class LeaseServiceScopingTests
{
    private const int TenantA    = 10;
    private const int TenantB    = 20;
    private const int OtherTenant = 99;

    private static readonly IEnumerable<LeaseDto> TenantALeases = new[]
    {
        MakeLease(id: 1, tenantId: TenantA),
        MakeLease(id: 2, tenantId: TenantA),
    };

    private static readonly IEnumerable<LeaseDto> TenantBLeases = new[]
    {
        MakeLease(id: 3, tenantId: TenantB),
        MakeLease(id: 4, tenantId: TenantB),
    };

    private static readonly IEnumerable<LeaseDto> AllLeases = new[]
    {
        MakeLease(id: 1, tenantId: TenantA),
        MakeLease(id: 2, tenantId: TenantA),
        MakeLease(id: 3, tenantId: TenantB),
        MakeLease(id: 4, tenantId: TenantB),
        MakeLease(id: 5, tenantId: OtherTenant),
    };

    // ── GetAllAsync ───────────────────────────────────────────────────────────

    [Fact]
    public async Task GetAllAsync_Viewer_OneTenant_ReturnsOnlyThatTenantLeases()
    {
        var (service, _) = BuildService(
            currentUser:      CurrentUserFactory.Viewer(),
            resolver:         OwnershipResolverFactory.ForTenants(TenantA),
            byTenantResults:  new() { [TenantA] = TenantALeases });

        var result = await service.GetAllAsync();

        Assert.Equal(2, result.Count());
        Assert.All(result, l => Assert.Equal(TenantA, l.TenantId));
    }

    [Fact]
    public async Task GetAllAsync_Viewer_TwoTenants_ReturnsCombinedLeases()
    {
        // Key test for the one-to-many scenario:
        // A Viewer who owns TenantA AND TenantB should see leases from BOTH.
        var (service, _) = BuildService(
            currentUser:     CurrentUserFactory.Viewer(),
            resolver:        OwnershipResolverFactory.ForTenants(TenantA, TenantB),
            byTenantResults: new()
            {
                [TenantA] = TenantALeases,
                [TenantB] = TenantBLeases,
            });

        var result = await service.GetAllAsync();

        Assert.Equal(4, result.Count());
        Assert.Contains(result, l => l.TenantId == TenantA);
        Assert.Contains(result, l => l.TenantId == TenantB);
        // Must NOT include the third unrelated tenant
        Assert.DoesNotContain(result, l => l.TenantId == OtherTenant);
    }

    [Fact]
    public async Task GetAllAsync_Viewer_NoTenants_ReturnsEmpty()
    {
        var (service, _) = BuildService(
            currentUser: CurrentUserFactory.Viewer(),
            resolver:    OwnershipResolverFactory.Empty());

        var result = await service.GetAllAsync();

        Assert.Empty(result);
    }

    [Fact]
    public async Task GetAllAsync_Admin_ReturnsAllLeases()
    {
        var (service, _) = BuildService(
            currentUser:   CurrentUserFactory.Admin(),
            resolver:      OwnershipResolverFactory.Bypass(),
            getAllResult:   AllLeases);

        var result = await service.GetAllAsync();

        Assert.Equal(5, result.Count());
    }

    [Fact]
    public async Task GetAllAsync_Manager_ReturnsAllLeases()
    {
        var (service, _) = BuildService(
            currentUser:  CurrentUserFactory.Manager(),
            resolver:     OwnershipResolverFactory.Bypass(),
            getAllResult:  AllLeases);

        var result = await service.GetAllAsync();

        Assert.Equal(5, result.Count());
    }

    // ── GetByIdAsync ──────────────────────────────────────────────────────────

    [Fact]
    public async Task GetByIdAsync_Viewer_OwnTenantLease_ReturnsLease()
    {
        var ownLease = MakeLease(id: 1, tenantId: TenantA);
        var (service, _) = BuildService(
            currentUser: CurrentUserFactory.Viewer(),
            resolver:    OwnershipResolverFactory.ForTenants(TenantA),
            byIdResult:  ownLease);

        var result = await service.GetByIdAsync(1);

        Assert.NotNull(result);
        Assert.Equal(1, result.Id);
    }

    [Fact]
    public async Task GetByIdAsync_Viewer_TwoTenants_CanAccessBoth()
    {
        // Viewer owning TenantA and TenantB should be able to get a lease from TenantB
        var tenantBLease = MakeLease(id: 3, tenantId: TenantB);
        var (service, _) = BuildService(
            currentUser: CurrentUserFactory.Viewer(),
            resolver:    OwnershipResolverFactory.ForTenants(TenantA, TenantB),
            byIdResult:  tenantBLease);

        var result = await service.GetByIdAsync(3);

        Assert.Equal(3, result.Id);
        Assert.Equal(TenantB, result.TenantId);
    }

    [Fact]
    public async Task GetByIdAsync_Viewer_UnownedTenantLease_ThrowsForbidden()
    {
        var otherLease = MakeLease(id: 5, tenantId: OtherTenant);
        var (service, _) = BuildService(
            currentUser: CurrentUserFactory.Viewer(),
            resolver:    OwnershipResolverFactory.ForTenants(TenantA, TenantB),
            byIdResult:  otherLease);

        await Assert.ThrowsAsync<ForbiddenAccessException>(
            () => service.GetByIdAsync(5));
    }

    [Fact]
    public async Task GetByIdAsync_Admin_AnyLease_ReturnsLease()
    {
        var anyLease = MakeLease(id: 5, tenantId: OtherTenant);
        var (service, _) = BuildService(
            currentUser: CurrentUserFactory.Admin(),
            resolver:    OwnershipResolverFactory.Bypass(),
            byIdResult:  anyLease);

        var result = await service.GetByIdAsync(5);

        Assert.Equal(5, result.Id);
    }

    [Fact]
    public async Task GetByIdAsync_NonExistentLease_ThrowsKeyNotFoundException()
    {
        var (service, _) = BuildService(
            currentUser: CurrentUserFactory.Admin(),
            resolver:    OwnershipResolverFactory.Bypass(),
            byIdResult:  null);

        await Assert.ThrowsAsync<KeyNotFoundException>(
            () => service.GetByIdAsync(999));
    }

    // ── GetByTenantIdAsync ────────────────────────────────────────────────────

    [Fact]
    public async Task GetByTenantIdAsync_Viewer_OwnTenantId_ReturnsLeases()
    {
        var (service, _) = BuildService(
            currentUser:     CurrentUserFactory.Viewer(),
            resolver:        OwnershipResolverFactory.ForTenants(TenantA),
            byTenantResults: new() { [TenantA] = TenantALeases });

        var result = await service.GetByTenantIdAsync(TenantA);

        Assert.Equal(2, result.Count());
    }

    [Fact]
    public async Task GetByTenantIdAsync_Viewer_UnownedTenantId_ThrowsForbidden()
    {
        var (service, _) = BuildService(
            currentUser: CurrentUserFactory.Viewer(),
            resolver:    OwnershipResolverFactory.ForTenants(TenantA));

        await Assert.ThrowsAsync<ForbiddenAccessException>(
            () => service.GetByTenantIdAsync(OtherTenant));
    }

    [Fact]
    public async Task GetByTenantIdAsync_Admin_AnyTenantId_ReturnsLeases()
    {
        var (service, _) = BuildService(
            currentUser:     CurrentUserFactory.Admin(),
            resolver:        OwnershipResolverFactory.Bypass(),
            byTenantResults: new() { [OtherTenant] = new[] { MakeLease(5, OtherTenant) } });

        var result = await service.GetByTenantIdAsync(OtherTenant);

        Assert.Single(result);
    }

    // ── Admin/Manager bypass: confirms null resolver = all records returned ───

    [Fact]
    public async Task GetAllAsync_Admin_Bypass_ReturnsAllRecordsRegardlessOfTenantOwnership()
    {
        // Explicitly proves the bypass contract: with null resolver,
        // Admin gets everything even though they own no tenants.
        var (service, leaseMock) = BuildService(
            currentUser: CurrentUserFactory.Admin(),
            resolver:    OwnershipResolverFactory.Bypass(),
            getAllResult: AllLeases);

        var result = await service.GetAllAsync();

        // Should have called GetAllAsync on the repo, NOT GetByTenantIdAsync
        leaseMock.Verify(r => r.GetAllAsync(), Times.Once);
        leaseMock.Verify(r => r.GetByTenantIdAsync(It.IsAny<int>()), Times.Never);
        Assert.Equal(5, result.Count());
    }

    // ── Helpers ───────────────────────────────────────────────────────────────

    private static (LeaseService service, Mock<ILeaseRepository> repoMock) BuildService(
        ICurrentUserService                  currentUser,
        ITenantOwnershipResolver             resolver,
        IEnumerable<LeaseDto>?               getAllResult     = null,
        Dictionary<int, IEnumerable<LeaseDto>>? byTenantResults = null,
        LeaseDto?                            byIdResult      = null)
    {
        var leaseMock = new Mock<ILeaseRepository>();
        var unitMock  = new Mock<IUnitRepository>();

        leaseMock
            .Setup(r => r.GetAllAsync())
            .ReturnsAsync(getAllResult ?? Enumerable.Empty<LeaseDto>());

        leaseMock
            .Setup(r => r.GetByTenantIdAsync(It.IsAny<int>()))
            .ReturnsAsync((int tid) =>
                byTenantResults?.TryGetValue(tid, out var leases) == true
                    ? leases
                    : Enumerable.Empty<LeaseDto>());

        leaseMock
            .Setup(r => r.GetByIdAsync(It.IsAny<int>()))
            .ReturnsAsync(byIdResult);

        var service = new LeaseService(leaseMock.Object, unitMock.Object, currentUser, resolver);
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
