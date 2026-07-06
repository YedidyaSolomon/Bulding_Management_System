using BMS.Application.DTOs.Invoices;
using BMS.Application.DTOs.Leases;
using BMS.Application.Interfaces;
using BMS.Application.Interfaces.Repositories;
using BMS.Application.Services;
using BMS.Tests.Helpers;
using Moq;
using Xunit;

namespace BMS.Tests.Services;

/// <summary>
/// Verifies that InvoiceService correctly enforces Viewer data scoping:
///
///   GetAllAsync   — Viewer receives only invoices for their own tenant.
///   GetAllAsync   — Admin/Manager receive all invoices.
///   GetByIdAsync  — Viewer accessing their own invoice succeeds.
///   GetByIdAsync  — Viewer accessing another tenant's invoice throws.
///   GetByIdAsync  — Admin accessing any invoice succeeds.
///   GetAllAsync   — Viewer with no tenant receives empty list.
/// </summary>
public class InvoiceServiceScopingTests
{
    private const int ViewerTenantId = 10;
    private const int OtherTenantId  = 99;

    // ─────────────────────────────────────────────────────────────────────────
    // GetAllAsync
    // ─────────────────────────────────────────────────────────────────────────

    [Fact]
    public async Task GetAllAsync_Viewer_ReturnsOnlyOwnTenantInvoices()
    {
        var viewerInvoices = MakeInvoiceList(ids: [1, 2]);
        var (service, _) = BuildService(
            currentUser: CurrentUserFactory.Viewer(tenantId: ViewerTenantId),
            byTenantResult: viewerInvoices);

        var result = await service.GetAllAsync();

        Assert.Equal(2, result.Count());
    }

    [Fact]
    public async Task GetAllAsync_Admin_ReturnsAllInvoices()
    {
        var all = MakeInvoiceList(ids: [1, 2, 3, 4]);
        var (service, _) = BuildService(
            currentUser: CurrentUserFactory.Admin(),
            getAllResult: all);

        var result = await service.GetAllAsync();

        Assert.Equal(4, result.Count());
    }

    [Fact]
    public async Task GetAllAsync_ViewerWithNoTenant_ReturnsEmpty()
    {
        var (service, _) = BuildService(
            currentUser: CurrentUserFactory.ViewerWithNoTenant());

        var result = await service.GetAllAsync();

        Assert.Empty(result);
    }

    // ─────────────────────────────────────────────────────────────────────────
    // GetByIdAsync — ownership enforcement via GetTenantIdForInvoiceAsync
    // ─────────────────────────────────────────────────────────────────────────

    [Fact]
    public async Task GetByIdAsync_Viewer_OwnInvoice_ReturnsInvoice()
    {
        var ownInvoice = MakeInvoice(id: 1);
        var (service, _) = BuildService(
            currentUser:          CurrentUserFactory.Viewer(tenantId: ViewerTenantId),
            byIdResult:           ownInvoice,
            tenantIdForInvoice:   ViewerTenantId);   // DB says this invoice belongs to Viewer's tenant

        var result = await service.GetByIdAsync(1);

        Assert.NotNull(result);
        Assert.Equal(1, result.Id);
    }

    [Fact]
    public async Task GetByIdAsync_Viewer_OtherTenantInvoice_ThrowsKeyNotFoundException()
    {
        var otherInvoice = MakeInvoice(id: 5);
        var (service, _) = BuildService(
            currentUser:          CurrentUserFactory.Viewer(tenantId: ViewerTenantId),
            byIdResult:           otherInvoice,
            tenantIdForInvoice:   OtherTenantId);   // belongs to a DIFFERENT tenant

        await Assert.ThrowsAsync<KeyNotFoundException>(
            () => service.GetByIdAsync(5));
    }

    [Fact]
    public async Task GetByIdAsync_Admin_AnyInvoice_ReturnsInvoice()
    {
        var anyInvoice = MakeInvoice(id: 5);
        var (service, _) = BuildService(
            currentUser:          CurrentUserFactory.Admin(),
            byIdResult:           anyInvoice,
            tenantIdForInvoice:   OtherTenantId);

        var result = await service.GetByIdAsync(5);

        Assert.Equal(5, result.Id);
    }

    [Fact]
    public async Task GetByIdAsync_NonExistentInvoice_ThrowsKeyNotFoundException()
    {
        var (service, _) = BuildService(
            currentUser: CurrentUserFactory.Admin(),
            byIdResult:  null);

        await Assert.ThrowsAsync<KeyNotFoundException>(
            () => service.GetByIdAsync(999));
    }

    // ─────────────────────────────────────────────────────────────────────────
    // GetByLeaseIdAsync — ownership checked via lease lookup
    // ─────────────────────────────────────────────────────────────────────────

    [Fact]
    public async Task GetByLeaseIdAsync_Viewer_OwnLease_ReturnsInvoices()
    {
        var ownLeaseInvoices = MakeInvoiceList(ids: [1, 2]);
        var (service, _) = BuildService(
            currentUser:     CurrentUserFactory.Viewer(tenantId: ViewerTenantId),
            byLeaseResult:   ownLeaseInvoices,
            leaseForId:      MakeLease(tenantId: ViewerTenantId));  // lease is owned by Viewer

        var result = await service.GetByLeaseIdAsync(leaseId: 1);

        Assert.Equal(2, result.Count());
    }

    [Fact]
    public async Task GetByLeaseIdAsync_Viewer_OtherTenantLease_ThrowsKeyNotFoundException()
    {
        var (service, _) = BuildService(
            currentUser:  CurrentUserFactory.Viewer(tenantId: ViewerTenantId),
            leaseForId:   MakeLease(tenantId: OtherTenantId));   // lease belongs to someone else

        await Assert.ThrowsAsync<KeyNotFoundException>(
            () => service.GetByLeaseIdAsync(leaseId: 9));
    }

    // ─────────────────────────────────────────────────────────────────────────
    // Helpers
    // ─────────────────────────────────────────────────────────────────────────

    private static (InvoiceService service, Mock<IInvoiceRepository> repoMock) BuildService(
        ICurrentUserService        currentUser,
        IEnumerable<InvoiceDto>?   getAllResult       = null,
        IEnumerable<InvoiceDto>?   byTenantResult    = null,
        IEnumerable<InvoiceDto>?   byLeaseResult     = null,
        InvoiceDto?                byIdResult        = null,
        int?                       tenantIdForInvoice = null,
        LeaseDto?                  leaseForId        = null)
    {
        var invoiceMock = new Mock<IInvoiceRepository>();
        var leaseMock   = new Mock<ILeaseRepository>();

        invoiceMock.Setup(r => r.GetAllAsync())
            .ReturnsAsync(getAllResult ?? []);

        invoiceMock.Setup(r => r.GetByTenantIdAsync(It.IsAny<int>()))
            .ReturnsAsync(byTenantResult ?? []);

        invoiceMock.Setup(r => r.GetByLeaseIdAsync(It.IsAny<int>()))
            .ReturnsAsync(byLeaseResult ?? []);

        invoiceMock.Setup(r => r.GetByIdAsync(It.IsAny<int>()))
            .ReturnsAsync(byIdResult);

        invoiceMock.Setup(r => r.GetTenantIdForInvoiceAsync(It.IsAny<int>()))
            .ReturnsAsync(tenantIdForInvoice);

        // Lease repo — used by GetByLeaseIdAsync to resolve the owning tenant
        leaseMock.Setup(r => r.GetByIdAsync(It.IsAny<int>()))
            .ReturnsAsync(leaseForId);

        var service = new InvoiceService(invoiceMock.Object, leaseMock.Object, currentUser);
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
