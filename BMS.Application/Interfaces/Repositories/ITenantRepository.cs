using BMS.Application.DTOs.Tenants;

namespace BMS.Application.Interfaces.Repositories;

public interface ITenantRepository
{
    Task<TenantDto?>             GetByIdAsync(int id);
    Task<IEnumerable<TenantDto>> GetAllAsync();
    /// <summary>Returns all tenants linked to a specific user account.</summary>
    Task<IEnumerable<TenantDto>> GetByUserIdAsync(string userId);
    Task<TenantDto>              CreateAsync(CreateTenantDto dto, string userId);
    Task                         UpdateAsync(int id, UpdateTenantDto dto);
    Task                         DeleteAsync(int id);
    Task<bool>                   ExistsAsync(int id);
    Task<bool>                   IsTINTakenAsync(string tin, int? excludeId = null);
    Task<LegalDocumentDto>              AddDocumentAsync(CreateLegalDocumentDto dto);
    Task<IEnumerable<LegalDocumentDto>> GetDocumentsAsync(int tenantId);
}
