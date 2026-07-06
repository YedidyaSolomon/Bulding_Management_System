using BMS.Application.DTOs.Documents;
using BMS.Application.Interfaces.Repositories;
using BMS.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace BMS.Infrastructure.Repositories;

public class DocumentRepository : IDocumentRepository
{
    private readonly ApplicationDbContext _context;

    public DocumentRepository(ApplicationDbContext context) => _context = context;

    public async Task<IEnumerable<ExpiringDocumentDto>> GetExpiringAsync(int withinDays)
    {
        var now    = DateTime.UtcNow.Date;
        var cutoff = now.AddDays(withinDays);

        var documents = await _context.LegalDocuments
            .Include(d => d.Tenant)
            .Where(d =>
                d.ExpiryDate != null &&
                d.ExpiryDate.Value.Date >= now &&
                d.ExpiryDate.Value.Date <= cutoff)
            .OrderBy(d => d.ExpiryDate)
            .Select(d => new ExpiringDocumentDto
            {
                Id           = d.Id,
                TenantId     = d.TenantId,
                TenantName   = d.Tenant.OrganizationName,
                UserId       = d.Tenant.UserId,
                DocumentType = d.DocumentType.ToString(),
                ExpiryDate   = d.ExpiryDate!.Value,
            })
            .ToListAsync();

        return documents;
    }
}
