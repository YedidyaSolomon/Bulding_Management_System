using BMS.Application.Common;
using BMS.Application.DTOs.Tenants;
using BMS.Application.Exceptions;
using BMS.Application.Interfaces;
using BMS.Application.Interfaces.Repositories;

namespace BMS.Application.Services;

public class TenantService : ITenantService
{
    private readonly ITenantRepository   _tenantRepository;
    private readonly IUserRepository     _userRepository;
    private readonly ICurrentUserService _currentUser;

    public TenantService(
        ITenantRepository tenantRepository,
        IUserRepository userRepository,
        ICurrentUserService currentUser)
    {
        _tenantRepository = tenantRepository;
        _userRepository   = userRepository;
        _currentUser      = currentUser;
    }

    public async Task<IEnumerable<TenantDto>> GetAllAsync()
    {
        if (_currentUser.IsViewer)
        {
            if (!_currentUser.TenantId.HasValue)
                return Enumerable.Empty<TenantDto>();

            var tenant = await _tenantRepository.GetByIdAsync(_currentUser.TenantId.Value);
            return tenant is null ? Enumerable.Empty<TenantDto>() : new[] { tenant };
        }

        return await _tenantRepository.GetAllAsync();
    }

    public async Task<TenantDto> GetByIdAsync(int id)
    {
        var tenant = await _tenantRepository.GetByIdAsync(id);
        if (tenant is null)
            throw new KeyNotFoundException($"Tenant {id} not found.");

        DataScope.EnsureViewerTenantAccess(_currentUser, id);
        return tenant;
    }

    public async Task<IEnumerable<TenantDto>> GetByUserIdAsync(string userId)
    {
        var resolvedUserId = DataScope.ResolveUserId(_currentUser, userId);

        if (_currentUser.IsViewer && _currentUser.TenantId.HasValue)
        {
            var tenant = await _tenantRepository.GetByIdAsync(_currentUser.TenantId.Value);
            return tenant is null ? Enumerable.Empty<TenantDto>() : new[] { tenant };
        }

        return await _tenantRepository.GetByUserIdAsync(resolvedUserId);
    }

    public Task<IEnumerable<UserDto>> GetRegisteredUsersAsync()
    {
        if (_currentUser.IsViewer)
            throw new ForbiddenAccessException();

        return _userRepository.GetAllAsync();
    }

    public async Task<TenantDto> CreateAsync(CreateTenantDto dto)
    {
        var user = await _userRepository.FindByEmailAsync(dto.UserEmail.Trim().ToLowerInvariant());
        if (user is null)
            throw new InvalidOperationException(
                $"No registered account found for email '{dto.UserEmail}'. " +
                "The user must register before a tenant can be linked to them.");

        if (!user.IsActive)
            throw new InvalidOperationException(
                $"The account for '{dto.UserEmail}' is disabled and cannot be linked to a tenant.");

        if (await _tenantRepository.IsTINTakenAsync(dto.TIN))
            throw new InvalidOperationException($"TIN '{dto.TIN}' is already registered.");

        var tenant = await _tenantRepository.CreateAsync(dto, user.Id);
        await _userRepository.SetTenantIdAsync(user.Id, tenant.Id);
        return tenant;
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

    public Task<LegalDocumentDto> AddDocumentAsync(CreateLegalDocumentDto dto) =>
        _tenantRepository.AddDocumentAsync(dto);

    public async Task<IEnumerable<LegalDocumentDto>> GetDocumentsAsync(int tenantId)
    {
        DataScope.EnsureViewerTenantAccess(_currentUser, tenantId);
        return await _tenantRepository.GetDocumentsAsync(tenantId);
    }
}
