using BMS.API.Wrappers;
using BMS.Application.DTOs.Payments;
using BMS.Application.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace BMS.API.Controllers;

[ApiController]
[Route("api/payments")]
[Authorize]
public class PaymentsController : ControllerBase
{
    private readonly IPaymentService _paymentService;

    public PaymentsController(IPaymentService paymentService) => _paymentService = paymentService;

    /// <summary>GET /api/payments — full payment history</summary>
    [HttpGet]
    [ProducesResponseType(typeof(ApiResponse<IEnumerable<PaymentDto>>), 200)]
    public async Task<IActionResult> GetAll()
    {
        var payments = await _paymentService.GetAllAsync();
        return Ok(ApiResponse<IEnumerable<PaymentDto>>.Ok(payments));
    }

    /// <summary>GET /api/payments/{id}</summary>
    [HttpGet("{id:int}")]
    [ProducesResponseType(typeof(ApiResponse<PaymentDto>), 200)]
    [ProducesResponseType(typeof(ApiResponse<object>), 404)]
    public async Task<IActionResult> GetById(int id)
    {
        var payment = await _paymentService.GetByIdAsync(id);
        return Ok(ApiResponse<PaymentDto>.Ok(payment));
    }

    /// <summary>GET /api/payments/by-invoice/{invoiceId}</summary>
    [HttpGet("by-invoice/{invoiceId:int}")]
    [ProducesResponseType(typeof(ApiResponse<IEnumerable<PaymentDto>>), 200)]
    public async Task<IActionResult> GetByInvoice(int invoiceId)
    {
        var payments = await _paymentService.GetByInvoiceIdAsync(invoiceId);
        return Ok(ApiResponse<IEnumerable<PaymentDto>>.Ok(payments));
    }

    /// <summary>GET /api/payments/outstanding — invoices with outstanding balances</summary>
    [HttpGet("outstanding")]
    [ProducesResponseType(typeof(ApiResponse<IEnumerable<PaymentDto>>), 200)]
    public async Task<IActionResult> Outstanding()
    {
        // Outstanding = all payments (caller correlates with invoice status)
        // Full outstanding balance report is under /api/reports
        var payments = await _paymentService.GetAllAsync();
        return Ok(ApiResponse<IEnumerable<PaymentDto>>.Ok(payments,
            "Use /api/reports/revenue for full outstanding balance analysis."));
    }

    /// <summary>POST /api/payments — record a payment</summary>
    [HttpPost]
    [Authorize(Roles = "Admin,Manager")]
    [ProducesResponseType(typeof(ApiResponse<PaymentDto>), 201)]
    [ProducesResponseType(typeof(ApiResponse<object>), 400)]
    [ProducesResponseType(typeof(ApiResponse<object>), 404)]
    public async Task<IActionResult> Record([FromBody] CreatePaymentDto dto)
    {
        var payment = await _paymentService.RecordPaymentAsync(dto);
        return StatusCode(201, ApiResponse<PaymentDto>.Ok(payment, "Payment recorded successfully."));
    }
}
