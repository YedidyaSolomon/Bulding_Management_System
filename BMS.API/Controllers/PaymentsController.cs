using BMS.API.Wrappers;
using BMS.Application.DTOs.Payments;
using BMS.Application.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace BMS.API.Controllers;

[ApiController]
[Route("api/payments")]
[Authorize]
public class PaymentsController : ControllerBase
{
    private readonly IPaymentService _paymentService;

    public PaymentsController(IPaymentService paymentService) => _paymentService = paymentService;

    /// <summary>GET /api/payments — Admin/Manager: full payment history</summary>
    [HttpGet]
    [Authorize(Roles = "Admin,Manager")]
    [ProducesResponseType(typeof(ApiResponse<IEnumerable<PaymentDto>>), 200)]
    public async Task<IActionResult> GetAll()
    {
        var payments = await _paymentService.GetAllAsync();
        return Ok(ApiResponse<IEnumerable<PaymentDto>>.Ok(payments));
    }

    /// <summary>GET /api/payments/mine — all roles: only payments for the calling user's tenants</summary>
    [HttpGet("mine")]
    [ProducesResponseType(typeof(ApiResponse<IEnumerable<PaymentDto>>), 200)]
    public async Task<IActionResult> GetMine()
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier)
                     ?? throw new UnauthorizedAccessException("User ID not found in token.");

        var payments = await _paymentService.GetByUserIdAsync(userId);
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

    /// <summary>POST /api/payments — record a payment (Admin/Manager only)</summary>
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
