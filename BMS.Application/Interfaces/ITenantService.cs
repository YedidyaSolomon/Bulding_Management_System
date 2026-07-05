using BMS.Application.DTOs.Tenants;

namespace BMS.Application.Interfaces;

public interface ITenantService
{
    Task<IEnumerable<TenantDto>> GetAllAsync();
    Task<TenantDto>              GetByIdAsync(int id);
    Task<TenantDto>              CreateAsync(CreateTenantDto dto);
    Task<TenantDto>              UpdateAsync(int id, UpdateTenantDto dto);
    Task                         DeleteAsync(int id);
    Task<LegalDocumentDto>       AddDocumentAsync(CreateLegalDocumentDto dto);
    Task<IEnumerable<LegalDocumentDto>> GetDocumentsAsync(int tenantId);
}
