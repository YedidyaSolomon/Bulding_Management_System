using BMS.Application.DTOs.Reports;
using BMS.Application.Interfaces;
using BMS.Application.Interfaces.Repositories;
using BMS.Infrastructure.Data;
using BMS.Infrastructure.Enums;
using ClosedXML.Excel;
using Microsoft.EntityFrameworkCore;

namespace BMS.Infrastructure.Services;

/// <summary>
/// Implements all five report types and the Excel export endpoint.
/// Lives in BMS.Infrastructure (not Application) because it depends on
/// ClosedXML (an infrastructure concern) and queries ApplicationDbContext
/// directly for the more complex aggregations.
/// </summary>
public class ReportService : IReportService
{
    private readonly ApplicationDbContext _context;
    private readonly IUnitRepository      _unitRepository;
    private readonly ILeaseRepository     _leaseRepository;

    public ReportService(
        ApplicationDbContext context,
        IUnitRepository      unitRepository,
        ILeaseRepository     leaseRepository)
    {
        _context         = context;
        _unitRepository  = unitRepository;
        _leaseRepository = leaseRepository;
    }

    // ── 1. Occupancy ─────────────────────────────────────────────────────────

    public async Task<OccupancyReportDto> GetOccupancyReportAsync()
    {
        var units = (await _unitRepository.GetAllAsync()).ToList();

        var occupied  = units.Count(u => u.Status == "Occupied");
        var available = units.Count(u => u.Status == "Available");
        var total     = units.Count;

        var byFloor = units
            .GroupBy(u => u.FloorNumber)
            .OrderBy(g => g.Key)
            .Select(g => new FloorOccupancyDto
            {
                FloorNumber   = g.Key,
                TotalUnits    = g.Count(),
                OccupiedUnits = g.Count(u => u.Status == "Occupied"),
                VacantUnits   = g.Count(u => u.Status != "Occupied"),
            })
            .ToList();

        return new OccupancyReportDto
        {
            TotalUnits     = total,
            OccupiedUnits  = occupied,
            VacantUnits    = total - occupied,
            AvailableUnits = available,
            OccupancyRate  = total == 0 ? 0 : Math.Round((decimal)occupied / total * 100, 2),
            ByFloor        = byFloor,
        };
    }

    // ── 2. Revenue ───────────────────────────────────────────────────────────

    public async Task<RevenueReportDto> GetRevenueReportAsync()
    {
        var now          = DateTime.UtcNow;
        var currentMonth = now.Month;
        var currentYear  = now.Year;

        // Build monthly aggregates for the last 12 months using a single query
        var cutoff = new DateTime(now.Year, now.Month, 1).AddMonths(-11);

        var invoices = await _context.Invoices
            .Where(i => new DateTime(i.PeriodYear, i.PeriodMonth, 1) >= cutoff)
            .Select(i => new
            {
                i.PeriodYear,
                i.PeriodMonth,
                i.AmountDue,
                i.Status,
                TotalPaid = (decimal?)i.Payments.Sum(p => (decimal?)p.AmountPaid) ?? 0m,
            })
            .ToListAsync();

        var monthly = invoices
            .GroupBy(i => new { i.PeriodYear, i.PeriodMonth })
            .OrderBy(g => g.Key.PeriodYear).ThenBy(g => g.Key.PeriodMonth)
            .Select(g => new MonthlyRevenueDto
            {
                Year              = g.Key.PeriodYear,
                Month             = g.Key.PeriodMonth,
                ExpectedRevenue   = g.Sum(i => i.AmountDue),
                CollectedRevenue  = g.Sum(i => i.TotalPaid),
                OutstandingAmount = g.Sum(i => i.AmountDue) - g.Sum(i => i.TotalPaid),
            })
            .ToList();

        var thisMonth = monthly.FirstOrDefault(
            m => m.Year == currentYear && m.Month == currentMonth);

        var ytd = monthly
            .Where(m => m.Year == currentYear)
            .Sum(m => m.CollectedRevenue);

        var expectedThisMonth   = thisMonth?.ExpectedRevenue  ?? 0m;
        var collectedThisMonth  = thisMonth?.CollectedRevenue ?? 0m;
        var collectionRate      = expectedThisMonth == 0 ? 0 :
            Math.Round(collectedThisMonth / expectedThisMonth * 100, 2);

        return new RevenueReportDto
        {
            CollectedThisMonth = collectedThisMonth,
            ExpectedThisMonth  = expectedThisMonth,
            YearToDate         = ytd,
            CollectionRate     = collectionRate,
            Monthly            = monthly,
        };
    }

    // ── 3. Arrears ───────────────────────────────────────────────────────────

    public async Task<ArrearsReportDto> GetArrearsReportAsync()
    {
        var now = DateTime.UtcNow;

        var overdueInvoices = await _context.Invoices
            .Include(i => i.Lease)
                .ThenInclude(l => l.Tenant)
            .Include(i => i.Lease)
                .ThenInclude(l => l.Unit)
            .Where(i => i.Status == InvoiceStatus.Overdue)
            .ToListAsync();

        var grouped = overdueInvoices
            .GroupBy(i => i.Lease.TenantId)
            .Select(g =>
            {
                var first    = g.First();
                var payments = _context.Payments
                    .Where(p => g.Select(i => i.Id).Contains(p.InvoiceId))
                    .Sum(p => (decimal?)p.AmountPaid) ?? 0m;

                var totalOwed = g.Sum(i => i.AmountDue) - payments;
                var oldestDue = g.Min(i => i.DueDate);
                var daysOld   = Math.Max(0, (int)(now - oldestDue).TotalDays);

                return new TenantArrearDto
                {
                    TenantId        = g.Key,
                    TenantName      = first.Lease.Tenant.OrganizationName,
                    UnitNumber      = first.Lease.Unit.UnitNumber,
                    OverdueInvoices = g.Count(),
                    TotalOwed       = Math.Max(0, totalOwed),
                    OldestDueDays   = daysOld,
                };
            })
            .Where(a => a.TotalOwed > 0)
            .OrderByDescending(a => a.TotalOwed)
            .ToList();

        return new ArrearsReportDto
        {
            TotalOverdue     = grouped.Sum(a => a.TotalOwed),
            TenantsInArrears = grouped.Count,
            OverdueInvoices  = overdueInvoices.Count,
            Arrears          = grouped,
        };
    }

    // ── 4. Lease Expiry ───────────────────────────────────────────────────────

    public async Task<LeaseExpiryReportDto> GetLeaseExpiryReportAsync(int daysAhead = 30)
    {
        var expiring = (await _leaseRepository.GetExpiringAsync(daysAhead)).ToList();
        var now = DateTime.UtcNow.Date;

        var leases = expiring.Select(e => new ExpiringLeaseDto
        {
            LeaseId       = e.Id,
            UnitNumber    = e.UnitNumber,
            TenantName    = e.TenantName,
            EndDate       = e.EndDate,
            DaysRemaining = Math.Max(0, (int)(e.EndDate.Date - now).TotalDays),
            MonthlyRent   = 0m,   // ExpiringLeaseDto doesn't carry rent; add below
        }).ToList();

        // Enrich with MonthlyRent from the full lease record
        // (ExpiringLeaseDto is a lightweight projection — load rent separately)
        var leaseIds = expiring.Select(e => e.Id).ToList();
        var rents = await _context.Leases
            .Where(l => leaseIds.Contains(l.Id))
            .Select(l => new { l.Id, l.MonthlyRent })
            .ToListAsync();

        var rentMap = rents.ToDictionary(r => r.Id, r => r.MonthlyRent);
        foreach (var l in leases)
            l.MonthlyRent = rentMap.TryGetValue(l.LeaseId, out var r) ? r : 0m;

        return new LeaseExpiryReportDto
        {
            ExpiringCount = leases.Count,
            Leases        = leases.OrderBy(l => l.DaysRemaining).ToList(),
        };
    }

    // ── 5. Document Expiry ────────────────────────────────────────────────────

    public async Task<DocumentExpiryReportDto> GetDocumentExpiryReportAsync(int daysAhead = 30)
    {
        var now    = DateTime.UtcNow.Date;
        var cutoff = now.AddDays(daysAhead);

        var docs = await _context.LegalDocuments
            .Include(d => d.Tenant)
            .Where(d => d.ExpiryDate.HasValue
                     && d.ExpiryDate.Value.Date >= now
                     && d.ExpiryDate.Value.Date <= cutoff)
            .OrderBy(d => d.ExpiryDate)
            .ToListAsync();

        var result = docs.Select(d => new ExpiringDocumentDto
        {
            DocumentId    = d.Id,
            TenantName    = d.Tenant.OrganizationName,
            DocumentType  = d.DocumentType.ToString(),
            ExpiryDate    = d.ExpiryDate!.Value,
            DaysRemaining = Math.Max(0, (int)(d.ExpiryDate.Value.Date - now).TotalDays),
        }).ToList();

        return new DocumentExpiryReportDto
        {
            ExpiringCount = result.Count,
            Documents     = result,
        };
    }

    // ── 6. Excel Export ───────────────────────────────────────────────────────

    public async Task<byte[]> ExportToExcelAsync(string type, int? daysAhead = null)
    {
        using var wb = new XLWorkbook();

        switch (type.Trim().ToLowerInvariant())
        {
            case "occupancy":
                await BuildOccupancySheet(wb);
                break;

            case "revenue":
                await BuildRevenueSheet(wb);
                break;

            case "arrears":
                await BuildArrearsSheet(wb);
                break;

            case "lease-expiry":
                await BuildLeaseExpirySheet(wb, daysAhead ?? 30);
                break;

            case "document-expiry":
                await BuildDocumentExpirySheet(wb, daysAhead ?? 30);
                break;

            default:
                throw new ArgumentException(
                    $"Unknown report type '{type}'. " +
                    "Valid values: occupancy, revenue, arrears, lease-expiry, document-expiry.");
        }

        using var ms = new MemoryStream();
        wb.SaveAs(ms);
        return ms.ToArray();
    }

    // ── Excel worksheet builders ─────────────────────────────────────────────

    private async Task BuildOccupancySheet(XLWorkbook wb)
    {
        var data = await GetOccupancyReportAsync();
        var ws   = wb.Worksheets.Add("Occupancy");

        // Summary block
        SetTitle(ws, "A1", "Occupancy Report");
        SetHeaderRow(ws, "A3", "Total Units", "Occupied", "Vacant", "Occupancy Rate");
        var r = ws.Row(4);
        r.Cell(1).Value = data.TotalUnits;
        r.Cell(2).Value = data.OccupiedUnits;
        r.Cell(3).Value = data.VacantUnits;
        r.Cell(4).Value = data.OccupancyRate / 100;
        r.Cell(4).Style.NumberFormat.Format = "0.0%";

        // Per-floor detail
        SetHeaderRow(ws, "A7", "Floor", "Total Units", "Occupied", "Vacant", "Rate");
        int row = 8;
        foreach (var f in data.ByFloor)
        {
            var fr = ws.Row(row++);
            fr.Cell(1).Value = $"Floor {f.FloorNumber}";
            fr.Cell(2).Value = f.TotalUnits;
            fr.Cell(3).Value = f.OccupiedUnits;
            fr.Cell(4).Value = f.VacantUnits;
            fr.Cell(5).Value = f.TotalUnits == 0 ? 0 : (double)f.OccupiedUnits / f.TotalUnits;
            fr.Cell(5).Style.NumberFormat.Format = "0.0%";
        }

        FitColumns(ws, 5);
    }

    private async Task BuildRevenueSheet(XLWorkbook wb)
    {
        var data = await GetRevenueReportAsync();
        var ws   = wb.Worksheets.Add("Revenue");

        SetTitle(ws, "A1", "Revenue Report");
        SetHeaderRow(ws, "A3",
            "Collected This Month", "Expected This Month", "Year-to-Date", "Collection Rate");
        var r = ws.Row(4);
        SetCurrency(r.Cell(1), data.CollectedThisMonth);
        SetCurrency(r.Cell(2), data.ExpectedThisMonth);
        SetCurrency(r.Cell(3), data.YearToDate);
        r.Cell(4).Value = data.CollectionRate / 100;
        r.Cell(4).Style.NumberFormat.Format = "0.0%";

        SetHeaderRow(ws, "A7",
            "Year", "Month", "Expected Revenue", "Collected Revenue", "Outstanding");
        int row = 8;
        foreach (var m in data.Monthly)
        {
            var mr = ws.Row(row++);
            mr.Cell(1).Value = m.Year;
            mr.Cell(2).Value = new DateTime(m.Year, m.Month, 1).ToString("MMMM");
            SetCurrency(mr.Cell(3), m.ExpectedRevenue);
            SetCurrency(mr.Cell(4), m.CollectedRevenue);
            SetCurrency(mr.Cell(5), m.OutstandingAmount);
        }

        FitColumns(ws, 5);
    }

    private async Task BuildArrearsSheet(XLWorkbook wb)
    {
        var data = await GetArrearsReportAsync();
        var ws   = wb.Worksheets.Add("Arrears");

        SetTitle(ws, "A1", "Arrears Report");
        SetHeaderRow(ws, "A3", "Total Overdue", "Tenants in Arrears", "Overdue Invoices");
        var r = ws.Row(4);
        SetCurrency(r.Cell(1), data.TotalOverdue);
        r.Cell(2).Value = data.TenantsInArrears;
        r.Cell(3).Value = data.OverdueInvoices;

        SetHeaderRow(ws, "A7",
            "Tenant", "Unit", "Overdue Invoices", "Oldest Due (days)", "Total Owed");
        int row = 8;
        foreach (var a in data.Arrears)
        {
            var ar = ws.Row(row++);
            ar.Cell(1).Value = a.TenantName;
            ar.Cell(2).Value = a.UnitNumber;
            ar.Cell(3).Value = a.OverdueInvoices;
            ar.Cell(4).Value = a.OldestDueDays;
            SetCurrency(ar.Cell(5), a.TotalOwed);
        }

        FitColumns(ws, 5);
    }

    private async Task BuildLeaseExpirySheet(XLWorkbook wb, int daysAhead)
    {
        var data = await GetLeaseExpiryReportAsync(daysAhead);
        var ws   = wb.Worksheets.Add("Lease Expiry");

        SetTitle(ws, "A1", $"Lease Expiry Report — Next {daysAhead} Days");
        SetHeaderRow(ws, "A3",
            "Unit", "Tenant", "End Date", "Monthly Rent (ETB)", "Days Remaining");
        int row = 4;
        foreach (var l in data.Leases)
        {
            var lr = ws.Row(row++);
            lr.Cell(1).Value = l.UnitNumber;
            lr.Cell(2).Value = l.TenantName;
            lr.Cell(3).Value = l.EndDate;
            lr.Cell(3).Style.NumberFormat.Format = "DD-MMM-YYYY";
            SetCurrency(lr.Cell(4), l.MonthlyRent);
            lr.Cell(5).Value = l.DaysRemaining;
        }

        FitColumns(ws, 5);
    }

    private async Task BuildDocumentExpirySheet(XLWorkbook wb, int daysAhead)
    {
        var data = await GetDocumentExpiryReportAsync(daysAhead);
        var ws   = wb.Worksheets.Add("Document Expiry");

        SetTitle(ws, "A1", $"Document Expiry Report — Next {daysAhead} Days");
        SetHeaderRow(ws, "A3",
            "Tenant", "Document Type", "Expiry Date", "Days Remaining");
        int row = 4;
        foreach (var d in data.Documents)
        {
            var dr = ws.Row(row++);
            dr.Cell(1).Value = d.TenantName;
            dr.Cell(2).Value = d.DocumentType;
            dr.Cell(3).Value = d.ExpiryDate;
            dr.Cell(3).Style.NumberFormat.Format = "DD-MMM-YYYY";
            dr.Cell(4).Value = d.DaysRemaining;
        }

        FitColumns(ws, 4);
    }

    // ── ClosedXML helpers ────────────────────────────────────────────────────

    private static void SetTitle(IXLWorksheet ws, string cell, string text)
    {
        var c = ws.Cell(cell);
        c.Value = text;
        c.Style.Font.Bold     = true;
        c.Style.Font.FontSize = 14;
    }

    private static void SetHeaderRow(IXLWorksheet ws, string startCell, params string[] headers)
    {
        var addr = ws.Cell(startCell).Address;
        for (int i = 0; i < headers.Length; i++)
        {
            var c = ws.Cell(addr.RowNumber, addr.ColumnNumber + i);
            c.Value = headers[i];
            c.Style.Font.Bold            = true;
            c.Style.Fill.BackgroundColor = XLColor.FromHtml("#667EEA");
            c.Style.Font.FontColor       = XLColor.White;
            c.Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
            c.Style.Border.BottomBorder  = XLBorderStyleValues.Thin;
        }
    }

    private static void SetCurrency(IXLCell cell, decimal value)
    {
        cell.Value = value;
        cell.Style.NumberFormat.Format = "#,##0.00";
    }

    private static void FitColumns(IXLWorksheet ws, int columnCount)
    {
        for (int i = 1; i <= columnCount; i++)
            ws.Column(i).AdjustToContents();
    }
}
