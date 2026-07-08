using BMS.Application.Common;
using BMS.Application.DTOs.Payments;
using BMS.Application.Interfaces;
using BMS.Application.Interfaces.Repositories;

namespace BMS.Application.Services;

public class PaymentService : IPaymentService
{
    private readonly IPaymentRepository      _paymentRepository;
    private readonly IInvoiceRepository      _invoiceRepository;
    private readonly ICurrentUserService     _currentUser;
    private readonly ITenantOwnershipResolver _ownershipResolver;

    public PaymentService(
        IPaymentRepository      paymentRepository,
        IInvoiceRepository      invoiceRepository,
        ICurrentUserService     currentUser,
        ITenantOwnershipResolver ownershipResolver)
    {
        _paymentRepository = paymentRepository;
        _invoiceRepository = invoiceRepository;
        _currentUser       = currentUser;
        _ownershipResolver = ownershipResolver;
    }

    public async Task<IEnumerable<PaymentDto>> GetAllAsync()
    {
        var ownedIds = await _ownershipResolver.GetOwnedTenantIdsAsync();

        if (ownedIds is null)
            return await _paymentRepository.GetAllAsync();

        if (ownedIds.Count == 0)
            return Enumerable.Empty<PaymentDto>();

        var tasks   = ownedIds.Select(tid => _paymentRepository.GetByTenantIdAsync(tid));
        var results = await Task.WhenAll(tasks);
        return results.SelectMany(x => x);
    }

    public async Task<IEnumerable<PaymentDto>> GetByInvoiceIdAsync(int invoiceId)
    {
        await EnsureViewerCanAccessInvoiceAsync(invoiceId);
        return await _paymentRepository.GetByInvoiceIdAsync(invoiceId);
    }

    public async Task<IEnumerable<PaymentDto>> GetByUserIdAsync(string userId)
    {
        var resolvedUserId = DataScope.ResolveUserId(_currentUser, userId);
        return await _paymentRepository.GetByUserIdAsync(resolvedUserId);
    }

    public async Task<PaymentDto> GetByIdAsync(int id)
    {
        var payment = await _paymentRepository.GetByIdAsync(id);
        if (payment is null)
            throw new KeyNotFoundException($"Payment {id} not found.");

        var tenantId = await _paymentRepository.GetTenantIdForPaymentAsync(id);
        if (tenantId.HasValue)
        {
            var ownedIds = await _ownershipResolver.GetOwnedTenantIdsAsync();
            DataScope.EnsureViewerOwnedTenantAccess(ownedIds, tenantId.Value);
        }

        return payment;
    }

    public async Task<PaymentDto> RecordPaymentAsync(CreatePaymentDto dto)
    {
        if (!await _invoiceRepository.ExistsAsync(dto.InvoiceId))
            throw new KeyNotFoundException($"Invoice {dto.InvoiceId} not found.");

        var payment = await _paymentRepository.CreateAsync(dto);
        await _invoiceRepository.UpdateStatusAsync(dto.InvoiceId, "Paid");
        return payment;
    }

    private async Task EnsureViewerCanAccessInvoiceAsync(int invoiceId)
    {
        var ownedIds = await _ownershipResolver.GetOwnedTenantIdsAsync();
        if (ownedIds is null)
            return; // Admin / Manager bypass

        var tenantId = await _invoiceRepository.GetTenantIdForInvoiceAsync(invoiceId);
        if (!tenantId.HasValue)
            throw new KeyNotFoundException($"Invoice {invoiceId} not found.");

        DataScope.EnsureViewerOwnedTenantAccess(ownedIds, tenantId.Value);
    }
}
