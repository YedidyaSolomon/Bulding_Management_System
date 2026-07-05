using BMS.Application.DTOs.Tenants;
using BMS.Application.Interfaces;
using BMS.Application.Interfaces.Repositories;

namespace BMS.Application.Services;

public class TenantService : ITenantService
{
    private readonly ITenantRepository _tenantRepository;

    public TenantService(ITenantRepository tenantRepository)
    {
        _tenantRepository = tenantRepository;
    }

    public Task<IEnumerable<TenantDto>> GetAllAsync() =>
        _tenantRepository.GetAllAsync();

    public async Task<TenantDto> GetByIdAsync(int id)
    {
        var tenant = await _tenantRepository.GetByIdAsync(id);
        return tenant ?? throw new KeyNotFoundException($"Tenant {id} not found.");
    }

    public async Task<TenantDto> CreateAsync(CreateTenantDto dto)
    {
        if (await _tenantRepository.IsTINTakenAsync(dto.TIN))
            throw new InvalidOperationException($"TIN '{dto.TIN}' is already registered.");

        return await _tenantRepository.CreateAsync(dto);
    }

    public async Task<TenantDto> UpdateAsync(int id, UpdateTenantDto dto)
    {
        if (!await _tenantRepository.ExistsAsync(id))
            throw new KeyNotFoundException($"Tenant {id} not found.");

        if (await _tenantRepository.IsTINTakenAsync(dto.TIN, excludeId: id))
            throw new InvalidOperationException($"TIN '{dto.TIN}' is already registered.");

        await _tenantRepository.UpdateAsync(id, dto);
        return (await _tenantRepository.GetByIdAsync(id))!;
    }

    public async Task DeleteAsync(int id)
    {
        if (!await _tenantRepository.ExistsAsync(id))
            throw new KeyNotFoundException($"Tenant {id} not found.");

        await _tenantRepository.DeleteAsync(id);
    }

    public Task<LegalDocumentDto>              AddDocumentAsync(CreateLegalDocumentDto dto) =>
        _tenantRepository.AddDocumentAsync(dto);

    public Task<IEnumerable<LegalDocumentDto>> GetDocumentsAsync(int tenantId) =>
        _tenantRepository.GetDocumentsAsync(tenantId);
}
