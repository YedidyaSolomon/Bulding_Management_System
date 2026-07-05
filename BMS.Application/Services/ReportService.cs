using BMS.Application.DTOs.Reports;
using BMS.Application.Interfaces;
using BMS.Application.Interfaces.Repositories;

namespace BMS.Application.Services;

public class ReportService : IReportService
{
    private readonly IUnitRepository   _unitRepository;
    private readonly IInvoiceRepository _invoiceRepository;
    private readonly IPaymentRepository _paymentRepository;

    public ReportService(
        IUnitRepository    unitRepository,
        IInvoiceRepository invoiceRepository,
        IPaymentRepository paymentRepository)
    {
        _unitRepository    = unitRepository;
        _invoiceRepository = invoiceRepository;
        _paymentRepository = paymentRepository;
    }

    public async Task<OccupancyReportDto> GetOccupancyReportAsync()
    {
        var units    = (await _unitRepository.GetAllAsync()).ToList();
        var occupied = units.Count(u => u.Status == "Occupied");

        return new OccupancyReportDto
        {
            TotalUnits     = units.Count,
            OccupiedUnits  = occupied,
            AvailableUnits = units.Count(u => u.Status == "Available"),
            OccupancyRate  = units.Count == 0 ? 0 : Math.Round((decimal)occupied / units.Count * 100, 2),
            Units          = units.Select(u => new UnitOccupancyDto
            {
                UnitNumber  = u.UnitNumber,
                FloorNumber = u.FloorNumber,
                Status      = u.Status
            })
        };
    }

    public async Task<RevenueReportDto> GetRevenueReportAsync(int month, int year)
    {
        var invoices = (await _invoiceRepository.GetAllAsync())
            .Where(i => i.PeriodMonth == month && i.PeriodYear == year)
            .ToList();

        var totalExpected  = invoices.Sum(i => i.AmountDue);
        var totalCollected = 0m;

        var lines = new List<RevenueLineDto>();
        foreach (var invoice in invoices)
        {
            var payments = (await _paymentRepository.GetByInvoiceIdAsync(invoice.Id)).ToList();
            var paid     = payments.Sum(p => p.AmountPaid);
            totalCollected += paid;

            lines.Add(new RevenueLineDto
            {
                AmountDue     = invoice.AmountDue,
                AmountPaid    = paid,
                InvoiceStatus = invoice.Status
            });
        }

        return new RevenueReportDto
        {
            Month            = month,
            Year             = year,
            TotalExpected    = totalExpected,
            TotalCollected   = totalCollected,
            TotalOutstanding = totalExpected - totalCollected,
            Lines            = lines
        };
    }
}
