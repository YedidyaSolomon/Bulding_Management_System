using BMS.Application.Common;
using BMS.Application.DTOs.Payments;
using BMS.Application.Interfaces;
using BMS.Application.Interfaces.Repositories;

namespace BMS.Application.Services;

public class PaymentService : IPaymentService
{
    private readonly IPaymentRepository  _paymentRepository;
    private readonly IInvoiceRepository  _invoiceRepository;
    private readonly ICurrentUserService _currentUser;

    public PaymentService(
        IPaymentRepository paymentRepository,
        IInvoiceRepository invoiceRepository,
        ICurrentUserService currentUser)
    {
        _paymentRepository = paymentRepository;
        _invoiceRepository = invoiceRepository;
        _currentUser       = currentUser;
    }

    public async Task<IEnumerable<PaymentDto>> GetAllAsync()
    {
        if (_currentUser.IsViewer)
        {
            if (!_currentUser.TenantId.HasValue)
                return Enumerable.Empty<PaymentDto>();

            return await _paymentRepository.GetByTenantIdAsync(_currentUser.TenantId.Value);
        }

        return await _paymentRepository.GetAllAsync();
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
            DataScope.EnsureViewerTenantAccess(_currentUser, tenantId.Value);

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
        if (!_currentUser.IsViewer)
            return;

        var tenantId = await _invoiceRepository.GetTenantIdForInvoiceAsync(invoiceId);
        if (!tenantId.HasValue)
            throw new KeyNotFoundException($"Invoice {invoiceId} not found.");

        DataScope.EnsureViewerTenantAccess(_currentUser, tenantId.Value);
    }
}
