using BMS.Application.DTOs.Tenants;
using BMS.Application.Interfaces.Repositories;

namespace BMS.Application.Interfaces;

public interface ITenantService
{
    Task<IEnumerable<TenantDto>> GetAllAsync();
    Task<TenantDto>              GetByIdAsync(int id);
    /// <summary>Returns all tenants owned by a specific user.</summary>
    Task<IEnumerable<TenantDto>> GetByUserIdAsync(string userId);
    /// <summary>Returns all registered user accounts for the admin email-picker.</summary>
    Task<IEnumerable<UserDto>>   GetRegisteredUsersAsync();
    Task<TenantDto>              CreateAsync(CreateTenantDto dto);
    Task<TenantDto>              UpdateAsync(int id, UpdateTenantDto dto);
    Task                         DeleteAsync(int id);
    /// <summary>Links an existing tenant to a registered Viewer account (Admin/Manager only).</summary>
    Task<TenantDto>              LinkUserAsync(int tenantId, string appUserId, bool force = false);
    Task<LegalDocumentDto>       AddDocumentAsync(CreateLegalDocumentDto dto);
    Task<IEnumerable<LegalDocumentDto>> GetDocumentsAsync(int tenantId);
}
