using BMS.Application.DTOs.Dashboard;

namespace BMS.Application.Interfaces;

public interface IDashboardService
{
    Task<DashboardSummaryDto> GetSummaryAsync(string userId);
}
