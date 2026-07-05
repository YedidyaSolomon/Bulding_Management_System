using BMS.API.Wrappers;
using BMS.Application.DTOs.Dashboard;
using BMS.Application.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace BMS.API.Controllers;

[ApiController]
[Route("api/dashboard")]
[Authorize]
public class DashboardController : ControllerBase
{
    private readonly IDashboardService _dashboardService;

    public DashboardController(IDashboardService dashboardService) =>
        _dashboardService = dashboardService;

    /// <summary>GET /api/dashboard — owner KPI statistics</summary>
    [HttpGet]
    [ProducesResponseType(typeof(ApiResponse<DashboardSummaryDto>), 200)]
    public async Task<IActionResult> GetSummary()
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier) ?? string.Empty;
        var summary = await _dashboardService.GetSummaryAsync(userId);
        return Ok(ApiResponse<DashboardSummaryDto>.Ok(summary));
    }
}
