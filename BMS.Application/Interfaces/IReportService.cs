using BMS.Application.DTOs.Reports;

namespace BMS.Application.Interfaces;

public interface IReportService
{
    Task<OccupancyReportDto>      GetOccupancyReportAsync();
    Task<RevenueReportDto>        GetRevenueReportAsync();
    Task<ArrearsReportDto>        GetArrearsReportAsync();
    Task<LeaseExpiryReportDto>    GetLeaseExpiryReportAsync(int daysAhead = 30);
    Task<DocumentExpiryReportDto> GetDocumentExpiryReportAsync(int daysAhead = 30);

    /// <summary>
    /// Builds an .xlsx workbook for the given report type and returns it as a byte array.
    /// <paramref name="type"/> must be one of: occupancy, revenue, arrears, lease-expiry, document-expiry.
    /// Throws <see cref="ArgumentException"/> for unknown types.
    /// </summary>
    Task<byte[]> ExportToExcelAsync(string type, int? daysAhead = null);
}
