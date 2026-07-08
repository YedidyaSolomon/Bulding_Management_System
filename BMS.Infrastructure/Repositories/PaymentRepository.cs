using BMS.Application.DTOs.Payments;
using BMS.Application.Interfaces.Repositories;
using BMS.Infrastructure.Data;
using BMS.Infrastructure.Entities;
using BMS.Infrastructure.Enums;
using Microsoft.EntityFrameworkCore;

namespace BMS.Infrastructure.Repositories;

public class PaymentRepository : IPaymentRepository
{
    private readonly ApplicationDbContext _context;

    public PaymentRepository(ApplicationDbContext context) => _context = context;

    public async Task<PaymentDto?> GetByIdAsync(int id)
    {
        var p = await _context.Payments
            .Include(p => p.Invoice)
            .FirstOrDefaultAsync(p => p.Id == id);
        return p is null ? null : MapToDto(p);
    }

    public async Task<IEnumerable<PaymentDto>> GetAllAsync()
    {
        var payments = await _context.Payments
            .Include(p => p.Invoice)
            .OrderByDescending(p => p.PaymentDate)
            .ToListAsync();
        return payments.Select(MapToDto);
    }

    public async Task<IEnumerable<PaymentDto>> GetByInvoiceIdAsync(int invoiceId)
    {
        var payments = await _context.Payments
            .Include(p => p.Invoice)
            .Where(p => p.InvoiceId == invoiceId)
            .OrderByDescending(p => p.PaymentDate)
            .ToListAsync();
        return payments.Select(MapToDto);
    }

    public async Task<IEnumerable<PaymentDto>> GetByUserIdAsync(string userId)
    {
        // Payment → Invoice → Lease → Tenant (UserId)
        var payments = await _context.Payments
            .Include(p => p.Invoice)
                .ThenInclude(i => i.Lease)
                    .ThenInclude(l => l.Tenant)
            .Where(p => p.Invoice.Lease.Tenant.AppUserId == userId)
            .OrderByDescending(p => p.PaymentDate)
            .ToListAsync();
        return payments.Select(MapToDto);
    }

    public async Task<IEnumerable<PaymentDto>> GetByTenantIdAsync(int tenantId)
    {
        var payments = await _context.Payments
            .Include(p => p.Invoice)
                .ThenInclude(i => i.Lease)
            .Where(p => p.Invoice.Lease.TenantId == tenantId)
            .OrderByDescending(p => p.PaymentDate)
            .ToListAsync();
        return payments.Select(MapToDto);
    }

    public async Task<int?> GetTenantIdForPaymentAsync(int paymentId)
    {
        return await _context.Payments
            .Where(p => p.Id == paymentId)
            .Select(p => (int?)p.Invoice.Lease.TenantId)
            .FirstOrDefaultAsync();
    }

    public async Task<PaymentDto> CreateAsync(CreatePaymentDto dto)
    {
        var payment = new Payment
        {
            InvoiceId       = dto.InvoiceId,
            AmountPaid      = dto.AmountPaid,
            PaymentDate     = dto.PaymentDate,
            PaymentMethod   = Enum.Parse<PaymentMethod>(dto.PaymentMethod, ignoreCase: true),
            ReferenceNumber = dto.ReferenceNumber,
            Notes           = dto.Notes
        };

        _context.Payments.Add(payment);
        await _context.SaveChangesAsync();

        // Reload with invoice for the response DTO
        return (await GetByIdAsync(payment.Id))!;
    }

    public Task<bool> ExistsAsync(int id) =>
        _context.Payments.AnyAsync(p => p.Id == id);

    // ── Mapping ─────────────────────────────────────────────────────────────

    private static PaymentDto MapToDto(Payment p) => new()
    {
        Id              = p.Id,
        InvoiceId       = p.InvoiceId,
        InvoiceNumber   = p.Invoice?.InvoiceNumber ?? string.Empty,
        AmountPaid      = p.AmountPaid,
        PaymentDate     = p.PaymentDate,
        PaymentMethod   = p.PaymentMethod.ToString(),
        ReferenceNumber = p.ReferenceNumber,
        Notes           = p.Notes
    };
}
