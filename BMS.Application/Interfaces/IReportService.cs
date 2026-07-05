using BMS.Application.DTOs.Reports;

namespace BMS.Application.Interfaces;

public interface IReportService
{
    Task<OccupancyReportDto> GetOccupancyReportAsync();
    Task<RevenueReportDto>   GetRevenueReportAsync(int month, int year);
}
