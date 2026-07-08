using BMS.API.Wrappers;
using BMS.Application.DTOs.Reports;
using BMS.Application.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace BMS.API.Controllers;

[ApiController]
[Route("api/reports")]
[Authorize(Roles = "Admin,Manager")]
public class ReportsController : ControllerBase
{
    private readonly IReportService _reportService;

    public ReportsController(IReportService reportService) => _reportService = reportService;

    // ── 1. Occupancy ─────────────────────────────────────────────────────────

    /// <summary>GET /api/reports/occupancy — occupancy by floor and overall</summary>
    [HttpGet("occupancy")]
    [ProducesResponseType(typeof(ApiResponse<OccupancyReportDto>), 200)]
    public async Task<IActionResult> Occupancy()
    {
        var report = await _reportService.GetOccupancyReportAsync();
        return Ok(ApiResponse<OccupancyReportDto>.Ok(report));
    }

    // ── 2. Revenue ────────────────────────────────────────────────────────────

    /// <summary>
    /// GET /api/reports/revenue
    /// Returns collected vs expected revenue for the current month plus the
    /// last 12 months of history.
    /// </summary>
    [HttpGet("revenue")]
    [ProducesResponseType(typeof(ApiResponse<RevenueReportDto>), 200)]
    public async Task<IActionResult> Revenue()
    {
        var report = await _reportService.GetRevenueReportAsync();
        return Ok(ApiResponse<RevenueReportDto>.Ok(report));
    }

    // ── 3. Arrears ────────────────────────────────────────────────────────────

    /// <summary>
    /// GET /api/reports/arrears
    /// Returns tenants with overdue invoices, total owed, and days outstanding.
    /// </summary>
    [HttpGet("arrears")]
    [ProducesResponseType(typeof(ApiResponse<ArrearsReportDto>), 200)]
    public async Task<IActionResult> Arrears()
    {
        var report = await _reportService.GetArrearsReportAsync();
        return Ok(ApiResponse<ArrearsReportDto>.Ok(report));
    }

    // ── 4. Lease Expiry ───────────────────────────────────────────────────────

    /// <summary>
    /// GET /api/reports/lease-expiry?daysAhead=30
    /// Returns active leases expiring within the next N days (default 30).
    /// </summary>
    [HttpGet("lease-expiry")]
    [ProducesResponseType(typeof(ApiResponse<LeaseExpiryReportDto>), 200)]
    [ProducesResponseType(typeof(ApiResponse<object>), 400)]
    public async Task<IActionResult> LeaseExpiry([FromQuery] int daysAhead = 30)
    {
        if (daysAhead < 1 || daysAhead > 365)
            return BadRequest(ApiResponse<object>.Fail("daysAhead must be between 1 and 365."));

        var report = await _reportService.GetLeaseExpiryReportAsync(daysAhead);
        return Ok(ApiResponse<LeaseExpiryReportDto>.Ok(report));
    }

    // ── 5. Document Expiry ────────────────────────────────────────────────────

    /// <summary>
    /// GET /api/reports/document-expiry?daysAhead=30
    /// Returns legal documents expiring within the next N days (default 30).
    /// </summary>
    [HttpGet("document-expiry")]
    [ProducesResponseType(typeof(ApiResponse<DocumentExpiryReportDto>), 200)]
    [ProducesResponseType(typeof(ApiResponse<object>), 400)]
    public async Task<IActionResult> DocumentExpiry([FromQuery] int daysAhead = 30)
    {
        if (daysAhead < 1 || daysAhead > 365)
            return BadRequest(ApiResponse<object>.Fail("daysAhead must be between 1 and 365."));

        var report = await _reportService.GetDocumentExpiryReportAsync(daysAhead);
        return Ok(ApiResponse<DocumentExpiryReportDto>.Ok(report));
    }

    // ── 6. Excel Export ───────────────────────────────────────────────────────

    /// <summary>
    /// GET /api/reports/export/{type}?daysAhead=30
    /// Downloads an Excel (.xlsx) file for the given report type.
    /// type = occupancy | revenue | arrears | lease-expiry | document-expiry
    /// daysAhead applies to lease-expiry and document-expiry only (default 30).
    /// </summary>
    [HttpGet("export/{type}")]
    [ProducesResponseType(typeof(FileContentResult), 200)]
    [ProducesResponseType(typeof(ApiResponse<object>), 400)]
    public async Task<IActionResult> Export(string type, [FromQuery] int daysAhead = 30)
    {
        var validTypes = new[] { "occupancy", "revenue", "arrears", "lease-expiry", "document-expiry" };
        if (!validTypes.Contains(type.Trim().ToLowerInvariant()))
            return BadRequest(ApiResponse<object>.Fail(
                $"Unknown report type '{type}'. " +
                "Valid values: occupancy, revenue, arrears, lease-expiry, document-expiry."));

        if (daysAhead < 1 || daysAhead > 365)
            return BadRequest(ApiResponse<object>.Fail("daysAhead must be between 1 and 365."));

        var bytes    = await _reportService.ExportToExcelAsync(type, daysAhead);
        var filename = $"{type}-report-{DateTime.UtcNow:yyyyMMdd}.xlsx";
        const string contentType = "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet";

        return File(bytes, contentType, filename);
    }
}
