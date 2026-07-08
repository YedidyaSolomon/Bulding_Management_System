using BMS.Application.DTOs.Payments;
using BMS.Application.Exceptions;
using BMS.Application.Interfaces;
using BMS.Application.Interfaces.Repositories;
using BMS.Application.Services;
using BMS.Tests.Helpers;
using Moq;
using Xunit;

namespace BMS.Tests.Services;

/// <summary>
/// Verifies that PaymentService correctly enforces Viewer data scoping
/// using the new ITenantOwnershipResolver architecture.
///
///   GetAllAsync   — Viewer with one tenant sees only that tenant's payments.
///   GetAllAsync   — Viewer with TWO tenants sees combined payments from both.
///   GetAllAsync   — Viewer with no tenants receives empty list.
///   GetAllAsync   — Admin/Manager receives all payments.
///   GetByIdAsync  — Viewer accessing own payment succeeds.
///   GetByIdAsync  — Viewer accessing other tenant's payment throws 403.
///   GetByIdAsync  — Admin accessing any payment succeeds.
/// </summary>
public class PaymentServiceScopingTests
{
    private const int TenantA    = 10;
    private const int TenantB    = 20;
    private const int OtherTenant = 99;

    // ── GetAllAsync ───────────────────────────────────────────────────────────

    [Fact]
    public async Task GetAllAsync_Viewer_OneTenant_ReturnsOnlyOwnTenantPayments()
    {
        var viewerPayments = MakePayments([1, 2]);
        var (service, _) = BuildService(
            currentUser:     CurrentUserFactory.Viewer(),
            resolver:        OwnershipResolverFactory.ForTenants(TenantA),
            byTenantResults: new() { [TenantA] = viewerPayments });

        var result = await service.GetAllAsync();

        Assert.Equal(2, result.Count());
    }

    [Fact]
    public async Task GetAllAsync_Viewer_TwoTenants_ReturnsCombinedPayments()
    {
        var tenantAPayments = MakePayments([1, 2]);
        var tenantBPayments = MakePayments([3, 4]);

        var (service, _) = BuildService(
            currentUser:     CurrentUserFactory.Viewer(),
            resolver:        OwnershipResolverFactory.ForTenants(TenantA, TenantB),
            byTenantResults: new()
            {
                [TenantA] = tenantAPayments,
                [TenantB] = tenantBPayments,
            });

        var result = await service.GetAllAsync();

        // Should see payments from BOTH owned tenants, but NOT from OtherTenant
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
    public async Task GetAllAsync_Admin_ReturnsAllPayments()
    {
        var all = MakePayments([1, 2, 3, 4]);
        var (service, _) = BuildService(
            currentUser: CurrentUserFactory.Admin(),
            resolver:    OwnershipResolverFactory.Bypass(),
            getAllResult: all);

        var result = await service.GetAllAsync();

        Assert.Equal(4, result.Count());
    }

    [Fact]
    public async Task GetAllAsync_Manager_ReturnsAllPayments()
    {
        var all = MakePayments([1, 2, 3]);
        var (service, _) = BuildService(
            currentUser: CurrentUserFactory.Manager(),
            resolver:    OwnershipResolverFactory.Bypass(),
            getAllResult: all);

        var result = await service.GetAllAsync();

        Assert.Equal(3, result.Count());
    }

    // ── GetByIdAsync ──────────────────────────────────────────────────────────

    [Fact]
    public async Task GetByIdAsync_Viewer_OwnPayment_ReturnsPayment()
    {
        var ownPayment = MakePayment(1);
        var (service, _) = BuildService(
            currentUser:        CurrentUserFactory.Viewer(),
            resolver:           OwnershipResolverFactory.ForTenants(TenantA),
            byIdResult:         ownPayment,
            tenantIdForPayment: TenantA);

        var result = await service.GetByIdAsync(1);

        Assert.NotNull(result);
        Assert.Equal(1, result.Id);
    }

    [Fact]
    public async Task GetByIdAsync_Viewer_TwoTenants_CanAccessPaymentFromEitherTenant()
    {
        var tenantBPayment = MakePayment(3);
        var (service, _) = BuildService(
            currentUser:        CurrentUserFactory.Viewer(),
            resolver:           OwnershipResolverFactory.ForTenants(TenantA, TenantB),
            byIdResult:         tenantBPayment,
            tenantIdForPayment: TenantB);

        var result = await service.GetByIdAsync(3);

        Assert.Equal(3, result.Id);
    }

    [Fact]
    public async Task GetByIdAsync_Viewer_UnownedTenantPayment_ThrowsForbidden()
    {
        var otherPayment = MakePayment(7);
        var (service, _) = BuildService(
            currentUser:        CurrentUserFactory.Viewer(),
            resolver:           OwnershipResolverFactory.ForTenants(TenantA, TenantB),
            byIdResult:         otherPayment,
            tenantIdForPayment: OtherTenant);

        await Assert.ThrowsAsync<ForbiddenAccessException>(
            () => service.GetByIdAsync(7));
    }

    [Fact]
    public async Task GetByIdAsync_Admin_AnyPayment_ReturnsPayment()
    {
        var anyPayment = MakePayment(7);
        var (service, _) = BuildService(
            currentUser:        CurrentUserFactory.Admin(),
            resolver:           OwnershipResolverFactory.Bypass(),
            byIdResult:         anyPayment,
            tenantIdForPayment: OtherTenant);

        var result = await service.GetByIdAsync(7);

        Assert.Equal(7, result.Id);
    }

    [Fact]
    public async Task GetByIdAsync_NonExistentPayment_ThrowsKeyNotFoundException()
    {
        var (service, _) = BuildService(
            currentUser: CurrentUserFactory.Admin(),
            resolver:    OwnershipResolverFactory.Bypass(),
            byIdResult:  null);

        await Assert.ThrowsAsync<KeyNotFoundException>(
            () => service.GetByIdAsync(999));
    }

    // ── Admin/Manager bypass ──────────────────────────────────────────────────

    [Fact]
    public async Task GetAllAsync_Admin_Bypass_CallsGetAllRepoMethod()
    {
        var all = MakePayments([1, 2, 3]);
        var (service, paymentMock) = BuildService(
            currentUser: CurrentUserFactory.Admin(),
            resolver:    OwnershipResolverFactory.Bypass(),
            getAllResult: all);

        await service.GetAllAsync();

        paymentMock.Verify(r => r.GetAllAsync(), Times.Once);
        paymentMock.Verify(r => r.GetByTenantIdAsync(It.IsAny<int>()), Times.Never);
    }

    // ── Helpers ───────────────────────────────────────────────────────────────

    private static (PaymentService service, Mock<IPaymentRepository> repoMock) BuildService(
        ICurrentUserService                       currentUser,
        ITenantOwnershipResolver                  resolver,
        IEnumerable<PaymentDto>?                  getAllResult       = null,
        Dictionary<int, IEnumerable<PaymentDto>>? byTenantResults   = null,
        PaymentDto?                               byIdResult        = null,
        int?                                      tenantIdForPayment = null)
    {
        var paymentMock = new Mock<IPaymentRepository>();
        var invoiceMock = new Mock<IInvoiceRepository>();

        paymentMock.Setup(r => r.GetAllAsync())
            .ReturnsAsync(getAllResult ?? []);

        paymentMock.Setup(r => r.GetByTenantIdAsync(It.IsAny<int>()))
            .ReturnsAsync((int tid) =>
                byTenantResults?.TryGetValue(tid, out var payments) == true
                    ? payments
                    : Enumerable.Empty<PaymentDto>());

        paymentMock.Setup(r => r.GetByIdAsync(It.IsAny<int>()))
            .ReturnsAsync(byIdResult);

        paymentMock.Setup(r => r.GetTenantIdForPaymentAsync(It.IsAny<int>()))
            .ReturnsAsync(tenantIdForPayment);

        invoiceMock.Setup(r => r.GetTenantIdForInvoiceAsync(It.IsAny<int>()))
            .ReturnsAsync(tenantIdForPayment);

        var service = new PaymentService(paymentMock.Object, invoiceMock.Object, currentUser, resolver);
        return (service, paymentMock);
    }

    private static PaymentDto MakePayment(int id) => new()
    {
        Id              = id,
        InvoiceId       = id * 10,
        InvoiceNumber   = $"INV-{id:D4}",
        AmountPaid      = 5000m,
        PaymentDate     = DateTime.UtcNow,
        PaymentMethod   = "Cash",
        ReferenceNumber = $"REF-{id:D4}",
        Notes           = null,
    };

    private static IEnumerable<PaymentDto> MakePayments(IEnumerable<int> ids) =>
        ids.Select(MakePayment).ToList();
}
