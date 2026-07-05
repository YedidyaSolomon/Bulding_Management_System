using BMS.Application.DTOs.Invoices;
using BMS.Application.Interfaces.Repositories;
using BMS.Infrastructure.Data;
using BMS.Infrastructure.Entities;
using BMS.Infrastructure.Enums;
using Microsoft.EntityFrameworkCore;

namespace BMS.Infrastructure.Repositories;

public class InvoiceRepository : IInvoiceRepository
{
    private readonly ApplicationDbContext _context;

    public InvoiceRepository(ApplicationDbContext context) => _context = context;

    public async Task<InvoiceDto?> GetByIdAsync(int id)
    {
        var inv = await _context.Invoices.FindAsync(id);
        return inv is null ? null : MapToDto(inv);
    }

    public async Task<IEnumerable<InvoiceDto>> GetAllAsync()
    {
        var invoices = await _context.Invoices
            .OrderByDescending(i => i.IssueDate)
            .ToListAsync();
        return invoices.Select(MapToDto);
    }

    public async Task<IEnumerable<InvoiceDto>> GetByLeaseIdAsync(int leaseId)
    {
        var invoices = await _context.Invoices
            .Where(i => i.LeaseId == leaseId)
            .OrderByDescending(i => i.IssueDate)
            .ToListAsync();
        return invoices.Select(MapToDto);
    }

    public async Task<IEnumerable<InvoiceDto>> GetOverdueAsync()
    {
        // Auto-mark overdue: any Issued invoice whose DueDate has passed
        var now = DateTime.UtcNow;
        var overdueList = await _context.Invoices
            .Where(i => i.Status == InvoiceStatus.Issued && i.DueDate < now)
            .ToListAsync();

        foreach (var inv in overdueList)
            inv.Status = InvoiceStatus.Overdue;

        if (overdueList.Any())
            await _context.SaveChangesAsync();

        var result = await _context.Invoices
            .Where(i => i.Status == InvoiceStatus.Overdue)
            .OrderBy(i => i.DueDate)
            .ToListAsync();

        return result.Select(MapToDto);
    }

    public async Task<InvoiceDto> CreateAsync(CreateInvoiceDto dto)
    {
        var invoiceNumber = await GenerateInvoiceNumberAsync();

        var invoice = new Invoice
        {
            LeaseId       = dto.LeaseId,
            InvoiceNumber = invoiceNumber,
            AmountDue     = dto.AmountDue,
            DueDate       = dto.DueDate,
            IssueDate     = DateTime.UtcNow,
            Status        = InvoiceStatus.Draft,
            PeriodMonth   = dto.PeriodMonth,
            PeriodYear    = dto.PeriodYear
        };

        _context.Invoices.Add(invoice);
        await _context.SaveChangesAsync();
        return MapToDto(invoice);
    }

    public async Task UpdateStatusAsync(int id, string status)
    {
        var invoice = await _context.Invoices.FindAsync(id)
                      ?? throw new KeyNotFoundException($"Invoice {id} not found.");

        invoice.Status = Enum.Parse<InvoiceStatus>(status, ignoreCase: true);
        await _context.SaveChangesAsync();
    }

    public Task<bool> ExistsAsync(int id) =>
        _context.Invoices.AnyAsync(i => i.Id == id);

    public async Task<string> GenerateInvoiceNumberAsync()
    {
        var count = await _context.Invoices.CountAsync();
        return $"INV-{DateTime.UtcNow:yyyyMM}-{(count + 1):D4}";
    }

    // ── Mapping ─────────────────────────────────────────────────────────────

    private static InvoiceDto MapToDto(Invoice i) => new()
    {
        Id            = i.Id,
        LeaseId       = i.LeaseId,
        InvoiceNumber = i.InvoiceNumber,
        AmountDue     = i.AmountDue,
        DueDate       = i.DueDate,
        IssueDate     = i.IssueDate,
        Status        = i.Status.ToString(),
        PeriodMonth   = i.PeriodMonth,
        PeriodYear    = i.PeriodYear
    };
}
