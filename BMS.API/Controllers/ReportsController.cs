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

    /// <summary>GET /api/reports/occupancy — occupancy per floor and overall</summary>
    [HttpGet("occupancy")]
    [ProducesResponseType(typeof(ApiResponse<OccupancyReportDto>), 200)]
    public async Task<IActionResult> Occupancy()
    {
        var report = await _reportService.GetOccupancyReportAsync();
        return Ok(ApiResponse<OccupancyReportDto>.Ok(report));
    }

    /// <summary>
    /// GET /api/reports/revenue?month=1&amp;year=2025
    /// Revenue collected vs expected for the given month/year.
    /// </summary>
    [HttpGet("revenue")]
    [ProducesResponseType(typeof(ApiResponse<RevenueReportDto>), 200)]
    [ProducesResponseType(typeof(ApiResponse<object>), 400)]
    public async Task<IActionResult> Revenue([FromQuery] int month, [FromQuery] int year)
    {
        if (month < 1 || month > 12)
            return BadRequest(ApiResponse<object>.Fail("Month must be between 1 and 12."));

        if (year < 2000 || year > DateTime.UtcNow.Year + 1)
            return BadRequest(ApiResponse<object>.Fail("Invalid year."));

        var report = await _reportService.GetRevenueReportAsync(month, year);
        return Ok(ApiResponse<RevenueReportDto>.Ok(report));
    }
}
