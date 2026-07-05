using BMS.Application.DTOs.Tenants;

namespace BMS.Application.Interfaces.Repositories;

public interface ITenantRepository
{
    Task<TenantDto?>             GetByIdAsync(int id);
    Task<IEnumerable<TenantDto>> GetAllAsync();
    Task<TenantDto>              CreateAsync(CreateTenantDto dto);
    Task                         UpdateAsync(int id, UpdateTenantDto dto);
    Task                         DeleteAsync(int id);
    Task<bool>                   ExistsAsync(int id);
    Task<bool>                   IsTINTakenAsync(string tin, int? excludeId = null);
    Task<LegalDocumentDto>              AddDocumentAsync(CreateLegalDocumentDto dto);
    Task<IEnumerable<LegalDocumentDto>> GetDocumentsAsync(int tenantId);
}
