using BMS.Application.DTOs.Payments;
using BMS.Application.Interfaces;
using BMS.Application.Interfaces.Repositories;
using BMS.Application.Services;
using BMS.Tests.Helpers;
using Moq;
using Xunit;

namespace BMS.Tests.Services;

/// <summary>
/// Verifies that PaymentService correctly enforces Viewer data scoping:
///
///   GetAllAsync   — Viewer sees only their own tenant's payments.
///   GetAllAsync   — Admin/Manager see all payments.
///   GetByIdAsync  — Viewer accessing own payment succeeds.
///   GetByIdAsync  — Viewer accessing other tenant's payment throws.
///   GetByIdAsync  — Admin accessing any payment succeeds.
///   GetAllAsync   — Viewer with no tenant receives empty list.
/// </summary>
public class PaymentServiceScopingTests
{
    private const int ViewerTenantId = 10;
    private const int OtherTenantId  = 99;

    // ─────────────────────────────────────────────────────────────────────────
    // GetAllAsync
    // ─────────────────────────────────────────────────────────────────────────

    [Fact]
    public async Task GetAllAsync_Viewer_ReturnsOnlyOwnTenantPayments()
    {
        var viewerPayments = MakePayments(ids: [1, 2]);
        var (service, _) = BuildService(
            currentUser:      CurrentUserFactory.Viewer(tenantId: ViewerTenantId),
            byTenantResult:   viewerPayments);

        var result = await service.GetAllAsync();

        Assert.Equal(2, result.Count());
    }

    [Fact]
    public async Task GetAllAsync_Admin_ReturnsAllPayments()
    {
        var all = MakePayments(ids: [1, 2, 3, 4]);
        var (service, _) = BuildService(
            currentUser:    CurrentUserFactory.Admin(),
            getAllResult:    all);

        var result = await service.GetAllAsync();

        Assert.Equal(4, result.Count());
    }

    [Fact]
    public async Task GetAllAsync_Manager_ReturnsAllPayments()
    {
        var all = MakePayments(ids: [1, 2, 3]);
        var (service, _) = BuildService(
            currentUser:  CurrentUserFactory.Manager(),
            getAllResult:  all);

        var result = await service.GetAllAsync();

        Assert.Equal(3, result.Count());
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
    // GetByIdAsync — ownership checked via GetTenantIdForPaymentAsync
    // ─────────────────────────────────────────────────────────────────────────

    [Fact]
    public async Task GetByIdAsync_Viewer_OwnPayment_ReturnsPayment()
    {
        var ownPayment = MakePayment(id: 1);
        var (service, _) = BuildService(
            currentUser:           CurrentUserFactory.Viewer(tenantId: ViewerTenantId),
            byIdResult:            ownPayment,
            tenantIdForPayment:    ViewerTenantId);

        var result = await service.GetByIdAsync(1);

        Assert.NotNull(result);
        Assert.Equal(1, result.Id);
    }

    [Fact]
    public async Task GetByIdAsync_Viewer_OtherTenantPayment_ThrowsKeyNotFoundException()
    {
        var otherPayment = MakePayment(id: 7);
        var (service, _) = BuildService(
            currentUser:           CurrentUserFactory.Viewer(tenantId: ViewerTenantId),
            byIdResult:            otherPayment,
            tenantIdForPayment:    OtherTenantId);   // payment belongs to a different tenant

        await Assert.ThrowsAsync<KeyNotFoundException>(
            () => service.GetByIdAsync(7));
    }

    [Fact]
    public async Task GetByIdAsync_Admin_AnyPayment_ReturnsPayment()
    {
        var anyPayment = MakePayment(id: 7);
        var (service, _) = BuildService(
            currentUser:           CurrentUserFactory.Admin(),
            byIdResult:            anyPayment,
            tenantIdForPayment:    OtherTenantId);

        var result = await service.GetByIdAsync(7);

        Assert.Equal(7, result.Id);
    }

    [Fact]
    public async Task GetByIdAsync_NonExistentPayment_ThrowsKeyNotFoundException()
    {
        var (service, _) = BuildService(
            currentUser: CurrentUserFactory.Admin(),
            byIdResult:  null);

        await Assert.ThrowsAsync<KeyNotFoundException>(
            () => service.GetByIdAsync(999));
    }

    // ─────────────────────────────────────────────────────────────────────────
    // Helpers
    // ─────────────────────────────────────────────────────────────────────────

    private static (PaymentService service, Mock<IPaymentRepository> repoMock) BuildService(
        ICurrentUserService        currentUser,
        IEnumerable<PaymentDto>?   getAllResult       = null,
        IEnumerable<PaymentDto>?   byTenantResult    = null,
        PaymentDto?                byIdResult        = null,
        int?                       tenantIdForPayment = null)
    {
        var paymentMock = new Mock<IPaymentRepository>();
        var invoiceMock = new Mock<IInvoiceRepository>();

        paymentMock.Setup(r => r.GetAllAsync())
            .ReturnsAsync(getAllResult ?? []);

        paymentMock.Setup(r => r.GetByTenantIdAsync(It.IsAny<int>()))
            .ReturnsAsync(byTenantResult ?? []);

        paymentMock.Setup(r => r.GetByIdAsync(It.IsAny<int>()))
            .ReturnsAsync(byIdResult);

        paymentMock.Setup(r => r.GetTenantIdForPaymentAsync(It.IsAny<int>()))
            .ReturnsAsync(tenantIdForPayment);

        // InvoiceRepository — used by GetByInvoiceIdAsync path; provide sensible defaults
        invoiceMock.Setup(r => r.GetTenantIdForInvoiceAsync(It.IsAny<int>()))
            .ReturnsAsync(tenantIdForPayment);   // same tenant as the payment for simplicity

        var service = new PaymentService(paymentMock.Object, invoiceMock.Object, currentUser);
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
