using BMS.API.Wrappers;
using BMS.Application.DTOs.Invoices;
using BMS.Application.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace BMS.API.Controllers;

[ApiController]
[Route("api/invoices")]
[Authorize]
public class InvoicesController : ControllerBase
{
    private readonly IInvoiceService _invoiceService;

    public InvoicesController(IInvoiceService invoiceService) => _invoiceService = invoiceService;

    /// <summary>GET /api/invoices</summary>
    [HttpGet]
    [ProducesResponseType(typeof(ApiResponse<IEnumerable<InvoiceDto>>), 200)]
    public async Task<IActionResult> GetAll()
    {
        var invoices = await _invoiceService.GetAllAsync();
        return Ok(ApiResponse<IEnumerable<InvoiceDto>>.Ok(invoices));
    }

    /// <summary>GET /api/invoices/{id}</summary>
    [HttpGet("{id:int}")]
    [ProducesResponseType(typeof(ApiResponse<InvoiceDto>), 200)]
    [ProducesResponseType(typeof(ApiResponse<object>), 404)]
    public async Task<IActionResult> GetById(int id)
    {
        var invoice = await _invoiceService.GetByIdAsync(id);
        return Ok(ApiResponse<InvoiceDto>.Ok(invoice));
    }

    /// <summary>GET /api/invoices/by-lease/{leaseId}</summary>
    [HttpGet("by-lease/{leaseId:int}")]
    [ProducesResponseType(typeof(ApiResponse<IEnumerable<InvoiceDto>>), 200)]
    public async Task<IActionResult> GetByLease(int leaseId)
    {
        var invoices = await _invoiceService.GetByLeaseIdAsync(leaseId);
        return Ok(ApiResponse<IEnumerable<InvoiceDto>>.Ok(invoices));
    }

    /// <summary>GET /api/invoices/overdue</summary>
    [HttpGet("overdue")]
    [ProducesResponseType(typeof(ApiResponse<IEnumerable<InvoiceDto>>), 200)]
    public async Task<IActionResult> GetOverdue()
    {
        var invoices = await _invoiceService.GetOverdueAsync();
        return Ok(ApiResponse<IEnumerable<InvoiceDto>>.Ok(invoices));
    }

    /// <summary>POST /api/invoices/generate — generate a new invoice</summary>
    [HttpPost("generate")]
    [Authorize(Roles = "Admin,Manager")]
    [ProducesResponseType(typeof(ApiResponse<InvoiceDto>), 201)]
    [ProducesResponseType(typeof(ApiResponse<object>), 400)]
    public async Task<IActionResult> Generate([FromBody] CreateInvoiceDto dto)
    {
        var invoice = await _invoiceService.CreateAsync(dto);
        return StatusCode(201, ApiResponse<InvoiceDto>.Ok(invoice, "Invoice generated successfully."));
    }

    /// <summary>PUT /api/invoices/{id}/issue — change status from Draft → Issued</summary>
    [HttpPut("{id:int}/issue")]
    [Authorize(Roles = "Admin,Manager")]
    [ProducesResponseType(typeof(ApiResponse<InvoiceDto>), 200)]
    [ProducesResponseType(typeof(ApiResponse<object>), 404)]
    public async Task<IActionResult> Issue(int id)
    {
        var invoice = await _invoiceService.IssueAsync(id);
        return Ok(ApiResponse<InvoiceDto>.Ok(invoice, "Invoice issued."));
    }

    /// <summary>PUT /api/invoices/{id}/cancel</summary>
    [HttpPut("{id:int}/cancel")]
    [Authorize(Roles = "Admin")]
    [ProducesResponseType(typeof(ApiResponse<InvoiceDto>), 200)]
    [ProducesResponseType(typeof(ApiResponse<object>), 404)]
    public async Task<IActionResult> Cancel(int id)
    {
        var invoice = await _invoiceService.CancelAsync(id);
        return Ok(ApiResponse<InvoiceDto>.Ok(invoice, "Invoice cancelled."));
    }
}
