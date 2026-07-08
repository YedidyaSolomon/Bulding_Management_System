using BMS.Application.DTOs.Tenants;
using BMS.Application.Interfaces.Repositories;
using BMS.Infrastructure.Data;
using BMS.Infrastructure.Entities;
using BMS.Infrastructure.Enums;
using Microsoft.EntityFrameworkCore;

namespace BMS.Infrastructure.Repositories;

public class TenantRepository : ITenantRepository
{
    private readonly ApplicationDbContext _context;

    public TenantRepository(ApplicationDbContext context) => _context = context;

    public async Task<TenantDto?> GetByIdAsync(int id)
    {
        var t = await _context.Tenants
            .Include(t => t.AppUser)
            .FirstOrDefaultAsync(t => t.Id == id);
        return t is null ? null : MapToDto(t);
    }

    public async Task<IEnumerable<TenantDto>> GetAllAsync()
    {
        var tenants = await _context.Tenants
            .Include(t => t.AppUser)
            .OrderBy(t => t.OrganizationName)
            .ToListAsync();
        return tenants.Select(MapToDto);
    }

    public async Task<IEnumerable<TenantDto>> GetByUserIdAsync(string userId)
    {
        var tenants = await _context.Tenants
            .Include(t => t.AppUser)
            .Where(t => t.AppUserId == userId)
            .OrderBy(t => t.OrganizationName)
            .ToListAsync();
        return tenants.Select(MapToDto);
    }

    public async Task<IEnumerable<TenantDto>> GetByIdsAsync(IEnumerable<int> ids)
    {
        var idList  = ids.ToList();
        var tenants = await _context.Tenants
            .Include(t => t.AppUser)
            .Where(t => idList.Contains(t.Id))
            .OrderBy(t => t.OrganizationName)
            .ToListAsync();
        return tenants.Select(MapToDto);
    }

    public async Task<TenantDto> CreateAsync(CreateTenantDto dto, string? appUserId)
    {
        var tenant = new Tenant
        {
            AppUserId         = string.IsNullOrWhiteSpace(appUserId) ? null : appUserId,
            OrganizationName  = dto.OrganizationName,
            TIN               = dto.TIN,
            Phone             = dto.Phone,
            BusinessType      = Enum.Parse<BusinessType>(dto.BusinessType, ignoreCase: true),
            ContactPersonName = dto.ContactPersonName,
            ContactEmail      = dto.ContactEmail,
            IsActive          = true
        };

        _context.Tenants.Add(tenant);
        await _context.SaveChangesAsync();

        // Reload with navigation so the DTO is complete
        return (await GetByIdAsync(tenant.Id))!;
    }

    public async Task UpdateAsync(int id, UpdateTenantDto dto)
    {
        var tenant = await _context.Tenants.FindAsync(id)
                     ?? throw new KeyNotFoundException($"Tenant {id} not found.");

        tenant.OrganizationName  = dto.OrganizationName;
        tenant.TIN               = dto.TIN;
        tenant.Phone             = dto.Phone;
        tenant.BusinessType      = Enum.Parse<BusinessType>(dto.BusinessType, ignoreCase: true);
        tenant.ContactPersonName = dto.ContactPersonName;
        tenant.ContactEmail      = dto.ContactEmail;
        tenant.IsActive          = dto.IsActive;

        await _context.SaveChangesAsync();
    }

    public async Task DeleteAsync(int id)
    {
        var tenant = await _context.Tenants.FindAsync(id)
                     ?? throw new KeyNotFoundException($"Tenant {id} not found.");

        // Soft-delete: deactivate instead of physical remove
        tenant.IsActive = false;
        await _context.SaveChangesAsync();
    }

    public Task<bool> ExistsAsync(int id) =>
        _context.Tenants.AnyAsync(t => t.Id == id);

    public Task<bool> IsTINTakenAsync(string tin, int? excludeId = null) =>
        excludeId.HasValue
            ? _context.Tenants.AnyAsync(t => t.TIN == tin && t.Id != excludeId.Value)
            : _context.Tenants.AnyAsync(t => t.TIN == tin);

    public async Task SetAppUserIdAsync(int tenantId, string appUserId)
    {
        var tenant = await _context.Tenants.FindAsync(tenantId)
                     ?? throw new KeyNotFoundException($"Tenant {tenantId} not found.");

        tenant.AppUserId = appUserId;
        await _context.SaveChangesAsync();
    }

    // ── Legal Documents ──────────────────────────────────────────────────────

    public async Task<LegalDocumentDto> AddDocumentAsync(CreateLegalDocumentDto dto)
    {
        if (!await ExistsAsync(dto.TenantId))
            throw new KeyNotFoundException($"Tenant {dto.TenantId} not found.");

        var doc = new LegalDocument
        {
            TenantId     = dto.TenantId,
            DocumentType = Enum.Parse<DocumentType>(dto.DocumentType, ignoreCase: true),
            FilePath     = dto.FilePath,
            ExpiryDate   = dto.ExpiryDate,
            UploadedAt   = DateTime.UtcNow,
            IsVerified   = false
        };

        _context.LegalDocuments.Add(doc);
        await _context.SaveChangesAsync();
        return MapDocToDto(doc);
    }

    public async Task<IEnumerable<LegalDocumentDto>> GetDocumentsAsync(int tenantId)
    {
        var docs = await _context.LegalDocuments
            .Where(d => d.TenantId == tenantId)
            .OrderByDescending(d => d.UploadedAt)
            .ToListAsync();

        return docs.Select(MapDocToDto);
    }

    // ── Mapping ─────────────────────────────────────────────────────────────

    private static TenantDto MapToDto(Tenant t) => new()
    {
        Id                = t.Id,
        AppUserId         = t.AppUserId,
        UserEmail         = t.AppUser?.Email,
        OrganizationName  = t.OrganizationName,
        TIN               = t.TIN,
        Phone             = t.Phone,
        BusinessType      = t.BusinessType.ToString(),
        ContactPersonName = t.ContactPersonName,
        ContactEmail      = t.ContactEmail,
        IsActive          = t.IsActive
    };

    private static LegalDocumentDto MapDocToDto(LegalDocument d) => new()
    {
        Id           = d.Id,
        TenantId     = d.TenantId,
        DocumentType = d.DocumentType.ToString(),
        FilePath     = d.FilePath,
        UploadedAt   = d.UploadedAt,
        ExpiryDate   = d.ExpiryDate,
        IsVerified   = d.IsVerified
    };
}
