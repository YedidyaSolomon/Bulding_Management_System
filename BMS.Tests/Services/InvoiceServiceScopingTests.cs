using BMS.Application.DTOs.Invoices;
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
/// Verifies that InvoiceService correctly enforces Viewer data scoping
/// using the new ITenantOwnershipResolver architecture.
///
///   GetAllAsync      — Viewer with one tenant sees only that tenant's invoices.
///   GetAllAsync      — Viewer with TWO tenants sees combined invoices from both.
///   GetAllAsync      — Viewer with no tenants receives empty list.
///   GetAllAsync      — Admin/Manager receives all invoices.
///   GetByIdAsync     — Viewer accessing own invoice succeeds.
///   GetByIdAsync     — Viewer accessing other tenant's invoice throws 403.
///   GetByIdAsync     — Admin accessing any invoice succeeds.
///   GetByLeaseIdAsync — Viewer on own lease succeeds; other lease throws 403.
/// </summary>
public class InvoiceServiceScopingTests
{
    private const int TenantA    = 10;
    private const int TenantB    = 20;
    private const int OtherTenant = 99;

    // ── GetAllAsync ───────────────────────────────────────────────────────────

    [Fact]
    public async Task GetAllAsync_Viewer_OneTenant_ReturnsOnlyOwnTenantInvoices()
    {
        var viewerInvoices = MakeInvoiceList([1, 2]);
        var (service, _) = BuildService(
            currentUser:     CurrentUserFactory.Viewer(),
            resolver:        OwnershipResolverFactory.ForTenants(TenantA),
            byTenantResults: new() { [TenantA] = viewerInvoices });

        var result = await service.GetAllAsync();

        Assert.Equal(2, result.Count());
    }

    [Fact]
    public async Task GetAllAsync_Viewer_TwoTenants_ReturnsCombinedInvoices()
    {
        var tenantAInvoices = MakeInvoiceList([1, 2]);
        var tenantBInvoices = MakeInvoiceList([3, 4]);

        var (service, _) = BuildService(
            currentUser:     CurrentUserFactory.Viewer(),
            resolver:        OwnershipResolverFactory.ForTenants(TenantA, TenantB),
            byTenantResults: new()
            {
                [TenantA] = tenantAInvoices,
                [TenantB] = tenantBInvoices,
            });

        var result = await service.GetAllAsync();

        Assert.Equal(4, result.Count());
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
    public async Task GetAllAsync_Admin_ReturnsAllInvoices()
    {
        var all = MakeInvoiceList([1, 2, 3, 4, 5]);
        var (service, _) = BuildService(
            currentUser: CurrentUserFactory.Admin(),
            resolver:    OwnershipResolverFactory.Bypass(),
            getAllResult: all);

        var result = await service.GetAllAsync();

        Assert.Equal(5, result.Count());
    }

    [Fact]
    public async Task GetAllAsync_Manager_ReturnsAllInvoices()
    {
        var all = MakeInvoiceList([1, 2, 3]);
        var (service, _) = BuildService(
            currentUser: CurrentUserFactory.Manager(),
            resolver:    OwnershipResolverFactory.Bypass(),
            getAllResult: all);

        var result = await service.GetAllAsync();

        Assert.Equal(3, result.Count());
    }

    // ── GetByIdAsync ──────────────────────────────────────────────────────────

    [Fact]
    public async Task GetByIdAsync_Viewer_OwnInvoice_ReturnsInvoice()
    {
        var ownInvoice = MakeInvoice(1);
        var (service, _) = BuildService(
            currentUser:       CurrentUserFactory.Viewer(),
            resolver:          OwnershipResolverFactory.ForTenants(TenantA),
            byIdResult:        ownInvoice,
            tenantIdForInvoice: TenantA);

        var result = await service.GetByIdAsync(1);

        Assert.NotNull(result);
        Assert.Equal(1, result.Id);
    }

    [Fact]
    public async Task GetByIdAsync_Viewer_TwoTenants_CanAccessInvoiceFromEitherTenant()
    {
        var tenantBInvoice = MakeInvoice(3);
        var (service, _) = BuildService(
            currentUser:       CurrentUserFactory.Viewer(),
            resolver:          OwnershipResolverFactory.ForTenants(TenantA, TenantB),
            byIdResult:        tenantBInvoice,
            tenantIdForInvoice: TenantB);

        var result = await service.GetByIdAsync(3);

        Assert.Equal(3, result.Id);
    }

    [Fact]
    public async Task GetByIdAsync_Viewer_UnownedTenantInvoice_ThrowsForbidden()
    {
        var otherInvoice = MakeInvoice(5);
        var (service, _) = BuildService(
            currentUser:       CurrentUserFactory.Viewer(),
            resolver:          OwnershipResolverFactory.ForTenants(TenantA, TenantB),
            byIdResult:        otherInvoice,
            tenantIdForInvoice: OtherTenant);

        await Assert.ThrowsAsync<ForbiddenAccessException>(
            () => service.GetByIdAsync(5));
    }

    [Fact]
    public async Task GetByIdAsync_Admin_AnyInvoice_ReturnsInvoice()
    {
        var anyInvoice = MakeInvoice(5);
        var (service, _) = BuildService(
            currentUser:       CurrentUserFactory.Admin(),
            resolver:          OwnershipResolverFactory.Bypass(),
            byIdResult:        anyInvoice,
            tenantIdForInvoice: OtherTenant);

        var result = await service.GetByIdAsync(5);

        Assert.Equal(5, result.Id);
    }

    [Fact]
    public async Task GetByIdAsync_NonExistentInvoice_ThrowsKeyNotFoundException()
    {
        var (service, _) = BuildService(
            currentUser: CurrentUserFactory.Admin(),
            resolver:    OwnershipResolverFactory.Bypass(),
            byIdResult:  null);

        await Assert.ThrowsAsync<KeyNotFoundException>(
            () => service.GetByIdAsync(999));
    }

    // ── GetByLeaseIdAsync ─────────────────────────────────────────────────────

    [Fact]
    public async Task GetByLeaseIdAsync_Viewer_OwnLease_ReturnsInvoices()
    {
        var ownLeaseInvoices = MakeInvoiceList([1, 2]);
        var (service, _) = BuildService(
            currentUser:   CurrentUserFactory.Viewer(),
            resolver:      OwnershipResolverFactory.ForTenants(TenantA),
            byLeaseResult: ownLeaseInvoices,
            leaseForId:    MakeLease(tenantId: TenantA));

        var result = await service.GetByLeaseIdAsync(leaseId: 1);

        Assert.Equal(2, result.Count());
    }

    [Fact]
    public async Task GetByLeaseIdAsync_Viewer_UnownedLease_ThrowsForbidden()
    {
        var (service, _) = BuildService(
            currentUser: CurrentUserFactory.Viewer(),
            resolver:    OwnershipResolverFactory.ForTenants(TenantA),
            leaseForId:  MakeLease(tenantId: OtherTenant));

        await Assert.ThrowsAsync<ForbiddenAccessException>(
            () => service.GetByLeaseIdAsync(leaseId: 9));
    }

    // ── TenantName + UnitNumber population ───────────────────────────────────

    [Fact]
    public async Task GetByIdAsync_Admin_ReturnsInvoiceWithPopulatedTenantNameAndUnitNumber()
    {
        // The repository is responsible for populating these via .Include().
        // This test verifies that InvoiceService passes the repo response through
        // unchanged — the populated fields must not be lost or reset.
        var invoiceWithDetails = new InvoiceDto
        {
            Id            = 42,
            LeaseId       = 7,
            InvoiceNumber = "INV-202601-0001",
            AmountDue     = 8500m,
            DueDate       = DateTime.UtcNow.AddDays(15),
            IssueDate     = DateTime.UtcNow,
            Status        = "Issued",
            PeriodMonth   = 6,
            PeriodYear    = 2026,
            // These are the fields that were previously always blank:
            TenantId      = 3,
            TenantName    = "Haset Business Group",
            UnitId        = 7,
            UnitNumber    = "Unit 201",
        };

        var (service, _) = BuildService(
            currentUser:        CurrentUserFactory.Admin(),
            resolver:           OwnershipResolverFactory.Bypass(),
            byIdResult:         invoiceWithDetails,
            tenantIdForInvoice: 3);

        var result = await service.GetByIdAsync(42);

        Assert.Equal("Haset Business Group", result.TenantName);
        Assert.Equal("Unit 201",             result.UnitNumber);
        Assert.Equal(3,                      result.TenantId);
        Assert.Equal(7,                      result.UnitId);
    }

    [Fact]
    public async Task GetByIdAsync_ReturnsEmptyStrings_WhenLeaseNavigationIsNull()
    {
        // Defensive test: if for any reason the repo returns an invoice where the
        // navigation wasn't loaded, tenant/unit fields default to empty string
        // (not null, which would cause NRE in the template).
        var invoiceNoDetails = new InvoiceDto
        {
            Id            = 99,
            LeaseId       = 1,
            InvoiceNumber = "INV-202601-0099",
            AmountDue     = 3000m,
            DueDate       = DateTime.UtcNow.AddDays(10),
            IssueDate     = DateTime.UtcNow,
            Status        = "Draft",
            PeriodMonth   = 1,
            PeriodYear    = 2026,
            TenantId      = 0,
            TenantName    = string.Empty,
            UnitId        = 0,
            UnitNumber    = string.Empty,
        };

        var (service, _) = BuildService(
            currentUser:        CurrentUserFactory.Admin(),
            resolver:           OwnershipResolverFactory.Bypass(),
            byIdResult:         invoiceNoDetails,
            tenantIdForInvoice: null);

        var result = await service.GetByIdAsync(99);

        Assert.Equal(string.Empty, result.TenantName);
        Assert.Equal(string.Empty, result.UnitNumber);
        // These should never be null — template uses || '—' but let's be explicit
        Assert.NotNull(result.TenantName);
        Assert.NotNull(result.UnitNumber);
    }

    // ── Admin/Manager bypass: Bypass resolver = all records visible ──────────

    [Fact]
    public async Task GetAllAsync_Admin_Bypass_CallsGetAllRepoMethod()
    {
        var all = MakeInvoiceList([1, 2, 3]);
        var (service, invoiceMock) = BuildService(
            currentUser: CurrentUserFactory.Admin(),
            resolver:    OwnershipResolverFactory.Bypass(),
            getAllResult: all);

        await service.GetAllAsync();

        invoiceMock.Verify(r => r.GetAllAsync(), Times.Once);
        invoiceMock.Verify(r => r.GetByTenantIdAsync(It.IsAny<int>()), Times.Never);
    }

    // ── Helpers ───────────────────────────────────────────────────────────────

    private static (InvoiceService service, Mock<IInvoiceRepository> repoMock) BuildService(
        ICurrentUserService                      currentUser,
        ITenantOwnershipResolver                 resolver,
        IEnumerable<InvoiceDto>?                 getAllResult       = null,
        Dictionary<int, IEnumerable<InvoiceDto>>? byTenantResults  = null,
        IEnumerable<InvoiceDto>?                 byLeaseResult     = null,
        InvoiceDto?                              byIdResult        = null,
        int?                                     tenantIdForInvoice = null,
        LeaseDto?                                leaseForId        = null)
    {
        var invoiceMock = new Mock<IInvoiceRepository>();
        var leaseMock   = new Mock<ILeaseRepository>();

        invoiceMock.Setup(r => r.GetAllAsync())
            .ReturnsAsync(getAllResult ?? []);

        invoiceMock.Setup(r => r.GetByTenantIdAsync(It.IsAny<int>()))
            .ReturnsAsync((int tid) =>
                byTenantResults?.TryGetValue(tid, out var inv) == true
                    ? inv
                    : Enumerable.Empty<InvoiceDto>());

        invoiceMock.Setup(r => r.GetByLeaseIdAsync(It.IsAny<int>()))
            .ReturnsAsync(byLeaseResult ?? []);

        invoiceMock.Setup(r => r.GetByIdAsync(It.IsAny<int>()))
            .ReturnsAsync(byIdResult);

        invoiceMock.Setup(r => r.GetTenantIdForInvoiceAsync(It.IsAny<int>()))
            .ReturnsAsync(tenantIdForInvoice);

        leaseMock.Setup(r => r.GetByIdAsync(It.IsAny<int>()))
            .ReturnsAsync(leaseForId);

        var service = new InvoiceService(invoiceMock.Object, leaseMock.Object, currentUser, resolver);
        return (service, invoiceMock);
    }

    private static InvoiceDto MakeInvoice(int id) => new()
    {
        Id            = id,
        LeaseId       = id * 10,
        InvoiceNumber = $"INV-{id:D4}",
        AmountDue     = 5000m,
        DueDate       = DateTime.UtcNow.AddDays(30),
        IssueDate     = DateTime.UtcNow,
        Status        = "Issued",
        PeriodMonth   = DateTime.UtcNow.Month,
        PeriodYear    = DateTime.UtcNow.Year,
        TenantId      = 10,
        TenantName    = "Sunrise Trading PLC",
        UnitId        = 5,
        UnitNumber    = "201",
    };

    private static IEnumerable<InvoiceDto> MakeInvoiceList(IEnumerable<int> ids) =>
        ids.Select(MakeInvoice).ToList();

    private static LeaseDto MakeLease(int tenantId) => new()
    {
        Id            = 1,
        TenantId      = tenantId,
        UnitId        = 10,
        UnitNumber    = "U010",
        TenantName    = $"Tenant {tenantId}",
        StartDate     = DateTime.UtcNow.AddMonths(-6),
        EndDate       = DateTime.UtcNow.AddMonths(6),
        MonthlyRent   = 5000m,
        DepositAmount = 10000m,
        Status        = "Active",
    };
}
