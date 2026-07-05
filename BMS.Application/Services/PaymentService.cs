using BMS.Application.DTOs.Payments;
using BMS.Application.Interfaces;
using BMS.Application.Interfaces.Repositories;

namespace BMS.Application.Services;

public class PaymentService : IPaymentService
{
    private readonly IPaymentRepository _paymentRepository;
    private readonly IInvoiceRepository _invoiceRepository;

    public PaymentService(IPaymentRepository paymentRepository, IInvoiceRepository invoiceRepository)
    {
        _paymentRepository = paymentRepository;
        _invoiceRepository = invoiceRepository;
    }

    public Task<IEnumerable<PaymentDto>> GetAllAsync()                      => _paymentRepository.GetAllAsync();
    public Task<IEnumerable<PaymentDto>> GetByInvoiceIdAsync(int invoiceId) => _paymentRepository.GetByInvoiceIdAsync(invoiceId);

    public async Task<PaymentDto> GetByIdAsync(int id)
    {
        var payment = await _paymentRepository.GetByIdAsync(id);
        return payment ?? throw new KeyNotFoundException($"Payment {id} not found.");
    }

    public async Task<PaymentDto> RecordPaymentAsync(CreatePaymentDto dto)
    {
        if (!await _invoiceRepository.ExistsAsync(dto.InvoiceId))
            throw new KeyNotFoundException($"Invoice {dto.InvoiceId} not found.");

        var payment = await _paymentRepository.CreateAsync(dto);

        // Mark invoice as Paid after recording payment
        await _invoiceRepository.UpdateStatusAsync(dto.InvoiceId, "Paid");

        return payment;
    }
}
