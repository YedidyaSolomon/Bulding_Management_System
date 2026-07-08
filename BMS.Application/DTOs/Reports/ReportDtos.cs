namespace BMS.Application.DTOs.Reports;

// ── Occupancy ─────────────────────────────────────────────────────────────────

public class OccupancyReportDto
{
    public int     TotalUnits     { get; set; }
    public int     OccupiedUnits  { get; set; }
    public int     VacantUnits    { get; set; }
    public int     AvailableUnits { get; set; }
    public decimal OccupancyRate  { get; set; }   // 0–100 %
    public IEnumerable<FloorOccupancyDto> ByFloor { get; set; } = [];
}

public class FloorOccupancyDto
{
    public int FloorNumber   { get; set; }
    public int TotalUnits    { get; set; }
    public int OccupiedUnits { get; set; }
    public int VacantUnits   { get; set; }
}

// ── Revenue ───────────────────────────────────────────────────────────────────

public class RevenueReportDto
{
    public decimal CollectedThisMonth { get; set; }
    public decimal ExpectedThisMonth  { get; set; }
    public decimal YearToDate         { get; set; }
    public decimal CollectionRate     { get; set; }   // 0–100 %
    public IEnumerable<MonthlyRevenueDto> Monthly { get; set; } = [];
}

public class MonthlyRevenueDto
{
    public int     Year               { get; set; }
    public int     Month              { get; set; }
    public decimal ExpectedRevenue    { get; set; }
    public decimal CollectedRevenue   { get; set; }
    public decimal OutstandingAmount  { get; set; }
}

// ── Arrears ───────────────────────────────────────────────────────────────────

public class ArrearsReportDto
{
    public decimal TotalOverdue      { get; set; }
    public int     TenantsInArrears  { get; set; }
    public int     OverdueInvoices   { get; set; }
    public IEnumerable<TenantArrearDto> Arrears { get; set; } = [];
}

public class TenantArrearDto
{
    public int     TenantId        { get; set; }
    public string  TenantName      { get; set; } = string.Empty;
    public string  UnitNumber      { get; set; } = string.Empty;
    public int     OverdueInvoices { get; set; }
    public decimal TotalOwed       { get; set; }
    public int     OldestDueDays   { get; set; }   // days since oldest unpaid invoice was due
}

// ── Lease Expiry ──────────────────────────────────────────────────────────────

public class LeaseExpiryReportDto
{
    public int ExpiringCount { get; set; }
    public IEnumerable<ExpiringLeaseDto> Leases { get; set; } = [];
}

public class ExpiringLeaseDto
{
    public int      LeaseId       { get; set; }
    public string   UnitNumber    { get; set; } = string.Empty;
    public string   TenantName    { get; set; } = string.Empty;
    public DateTime EndDate       { get; set; }
    public int      DaysRemaining { get; set; }
    public decimal  MonthlyRent   { get; set; }
}

// ── Document Expiry ───────────────────────────────────────────────────────────

public class DocumentExpiryReportDto
{
    public int ExpiringCount { get; set; }
    public IEnumerable<ExpiringDocumentDto> Documents { get; set; } = [];
}

public class ExpiringDocumentDto
{
    public int      DocumentId    { get; set; }
    public string   TenantName    { get; set; } = string.Empty;
    public string   DocumentType  { get; set; } = string.Empty;
    public DateTime ExpiryDate    { get; set; }
    public int      DaysRemaining { get; set; }
}
