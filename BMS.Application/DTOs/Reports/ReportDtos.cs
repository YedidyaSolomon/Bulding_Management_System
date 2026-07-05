namespace BMS.Application.DTOs.Reports;

public class OccupancyReportDto
{
    public int     TotalUnits     { get; set; }
    public int     OccupiedUnits  { get; set; }
    public int     AvailableUnits { get; set; }
    public decimal OccupancyRate  { get; set; }
    public IEnumerable<UnitOccupancyDto> Units { get; set; } = new List<UnitOccupancyDto>();
}

public class UnitOccupancyDto
{
    public string  UnitNumber  { get; set; } = string.Empty;
    public int     FloorNumber { get; set; }
    public string  Status      { get; set; } = string.Empty;
    public string? TenantName  { get; set; }
}

public class RevenueReportDto
{
    public int     Month           { get; set; }
    public int     Year            { get; set; }
    public decimal TotalExpected   { get; set; }
    public decimal TotalCollected  { get; set; }
    public decimal TotalOutstanding { get; set; }
    public IEnumerable<RevenueLineDto> Lines { get; set; } = new List<RevenueLineDto>();
}

public class RevenueLineDto
{
    public string  UnitNumber    { get; set; } = string.Empty;
    public string  TenantName    { get; set; } = string.Empty;
    public decimal AmountDue     { get; set; }
    public decimal AmountPaid    { get; set; }
    public string  InvoiceStatus { get; set; } = string.Empty;
}
