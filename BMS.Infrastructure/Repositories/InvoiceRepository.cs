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

    // ── Base query — always includes Lease → Tenant and Lease → Unit ─────────
    private IQueryable<Invoice> WithIncludes() =>
        _context.Invoices
            .Include(i => i.Lease)
                .ThenInclude(l => l.Tenant)
            .Include(i => i.Lease)
                .ThenInclude(l => l.Unit);

    // ─────────────────────────────────────────────────────────────────────────

    public async Task<InvoiceDto?> GetByIdAsync(int id)
    {
        var inv = await WithIncludes()
            .FirstOrDefaultAsync(i => i.Id == id);
        return inv is null ? null : MapToDto(inv);
    }

    public async Task<IEnumerable<InvoiceDto>> GetAllAsync()
    {
        var invoices = await WithIncludes()
            .OrderByDescending(i => i.IssueDate)
            .ToListAsync();
        return invoices.Select(MapToDto);
    }

    public async Task<IEnumerable<InvoiceDto>> GetByLeaseIdAsync(int leaseId)
    {
        var invoices = await WithIncludes()
            .Where(i => i.LeaseId == leaseId)
            .OrderByDescending(i => i.IssueDate)
            .ToListAsync();
        return invoices.Select(MapToDto);
    }

    public async Task<IEnumerable<InvoiceDto>> GetByUserIdAsync(string userId)
    {
        var invoices = await WithIncludes()
            .Where(i => i.Lease.Tenant.AppUserId == userId)
            .OrderByDescending(i => i.IssueDate)
            .ToListAsync();
        return invoices.Select(MapToDto);
    }

    public async Task<IEnumerable<InvoiceDto>> GetByTenantIdAsync(int tenantId)
    {
        var invoices = await WithIncludes()
            .Where(i => i.Lease.TenantId == tenantId)
            .OrderByDescending(i => i.IssueDate)
            .ToListAsync();
        return invoices.Select(MapToDto);
    }

    public async Task<int?> GetTenantIdForInvoiceAsync(int invoiceId)
    {
        return await _context.Invoices
            .Where(i => i.Id == invoiceId)
            .Select(i => (int?)i.Lease.TenantId)
            .FirstOrDefaultAsync();
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

        var result = await WithIncludes()
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
            PeriodYear    = dto.PeriodYear,
        };

        _context.Invoices.Add(invoice);
        await _context.SaveChangesAsync();

        // Reload with full includes so the returned DTO has tenant/unit info
        return (await GetByIdAsync(invoice.Id))!;
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
        PeriodYear    = i.PeriodYear,
        // Navigation properties — populated when WithIncludes() was used
        TenantId      = i.Lease?.TenantId   ?? 0,
        TenantName    = i.Lease?.Tenant?.OrganizationName ?? string.Empty,
        UnitId        = i.Lease?.UnitId     ?? 0,
        UnitNumber    = i.Lease?.Unit?.UnitNumber         ?? string.Empty,
    };
}
