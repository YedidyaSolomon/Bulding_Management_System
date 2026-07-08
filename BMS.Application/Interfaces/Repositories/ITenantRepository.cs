using BMS.Application.DTOs.Tenants;

namespace BMS.Application.Interfaces.Repositories;

public interface ITenantRepository
{
    Task<TenantDto?>             GetByIdAsync(int id);
    Task<IEnumerable<TenantDto>> GetAllAsync();
    /// <summary>Returns all tenants owned by a specific user (AppUserId match).</summary>
    Task<IEnumerable<TenantDto>> GetByUserIdAsync(string userId);
    /// <summary>Returns tenants whose IDs are in the supplied list (Viewer multi-tenant GetAll).</summary>
    Task<IEnumerable<TenantDto>> GetByIdsAsync(IEnumerable<int> ids);
    /// <summary>Creates a tenant; appUserId is nullable — tenant may be unlinked at creation time.</summary>
    Task<TenantDto>              CreateAsync(CreateTenantDto dto, string? appUserId);
    Task                         UpdateAsync(int id, UpdateTenantDto dto);
    Task                         DeleteAsync(int id);
    Task<bool>                   ExistsAsync(int id);
    Task<bool>                   IsTINTakenAsync(string tin, int? excludeId = null);
    /// <summary>Links an existing tenant to a user account (PUT /api/tenants/{id}/link-user).</summary>
    Task                         SetAppUserIdAsync(int tenantId, string appUserId);

    Task<LegalDocumentDto>              AddDocumentAsync(CreateLegalDocumentDto dto);
    Task<IEnumerable<LegalDocumentDto>> GetDocumentsAsync(int tenantId);
}
