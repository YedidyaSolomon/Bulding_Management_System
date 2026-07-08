using BMS.Application.Common;
using BMS.Application.DTOs.Tenants;
using BMS.Application.Exceptions;
using BMS.Application.Interfaces;
using BMS.Application.Interfaces.Repositories;

namespace BMS.Application.Services;

public class TenantService : ITenantService
{
    private readonly ITenantRepository        _tenantRepository;
    private readonly IUserRepository          _userRepository;
    private readonly ICurrentUserService      _currentUser;
    private readonly ITenantOwnershipResolver _ownershipResolver;

    public TenantService(
        ITenantRepository        tenantRepository,
        IUserRepository          userRepository,
        ICurrentUserService      currentUser,
        ITenantOwnershipResolver ownershipResolver)
    {
        _tenantRepository  = tenantRepository;
        _userRepository    = userRepository;
        _currentUser       = currentUser;
        _ownershipResolver = ownershipResolver;
    }

    public async Task<IEnumerable<TenantDto>> GetAllAsync()
    {
        var ownedIds = await _ownershipResolver.GetOwnedTenantIdsAsync();

        if (ownedIds is null)
            return await _tenantRepository.GetAllAsync();

        // Viewer — return only their own tenants
        if (ownedIds.Count == 0)
            return Enumerable.Empty<TenantDto>();

        return await _tenantRepository.GetByIdsAsync(ownedIds);
    }

    public async Task<TenantDto> GetByIdAsync(int id)
    {
        var tenant = await _tenantRepository.GetByIdAsync(id);
        if (tenant is null)
            throw new KeyNotFoundException($"Tenant {id} not found.");

        var ownedIds = await _ownershipResolver.GetOwnedTenantIdsAsync();
        DataScope.EnsureViewerOwnedTenantAccess(ownedIds, id);

        return tenant;
    }

    public async Task<IEnumerable<TenantDto>> GetByUserIdAsync(string userId)
    {
        var resolvedUserId = DataScope.ResolveUserId(_currentUser, userId);
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
        if (_currentUser.IsViewer)
            throw new ForbiddenAccessException();

        // If an AppUserId was provided, validate the user exists and has the Viewer role
        if (!string.IsNullOrWhiteSpace(dto.AppUserId))
        {
            var user = await _userRepository.FindByIdAsync(dto.AppUserId)
                       ?? throw new InvalidOperationException(
                           $"No registered account found with ID '{dto.AppUserId}'.");

            if (!user.IsActive)
                throw new InvalidOperationException(
                    $"The account for user '{dto.AppUserId}' is disabled and cannot be linked to a tenant.");

            var role = await _userRepository.GetRoleAsync(dto.AppUserId);
            if (!string.Equals(role, "Viewer", StringComparison.OrdinalIgnoreCase))
                throw new InvalidOperationException(
                    "Only Viewer accounts can be linked as tenant owners.");
        }

        if (await _tenantRepository.IsTINTakenAsync(dto.TIN))
            throw new InvalidOperationException($"TIN '{dto.TIN}' is already registered.");

        // No uniqueness constraint — same user can own multiple tenants
        return await _tenantRepository.CreateAsync(dto, dto.AppUserId);
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

    /// <summary>
    /// Links an existing Tenant to a registered Viewer account.
    /// Admin/Manager only — endpoint: PUT /api/tenants/{id}/link-user.
    /// </summary>
    /// <param name="force">
    /// When <c>false</c> (default), rejects the request if the tenant already has a
    /// non-null <c>AppUserId</c>, preventing silent overwrites.
    /// Pass <c>true</c> to explicitly re-link to a different user.
    /// </param>
    public async Task<TenantDto> LinkUserAsync(int tenantId, string appUserId, bool force = false)
    {
        if (_currentUser.IsViewer)
            throw new ForbiddenAccessException();

        var tenant = await _tenantRepository.GetByIdAsync(tenantId)
                     ?? throw new KeyNotFoundException($"Tenant {tenantId} not found.");

        // Guard against silent overwrite of an existing link
        if (!force && tenant.AppUserId is not null)
            throw new InvalidOperationException(
                $"Tenant {tenantId} is already linked to user '{tenant.AppUserId}'. " +
                "Pass force=true to overwrite the existing link.");

        var user = await _userRepository.FindByIdAsync(appUserId)
                   ?? throw new InvalidOperationException(
                       $"No registered account found with ID '{appUserId}'.");

        if (!user.IsActive)
            throw new InvalidOperationException(
                $"The account for user '{appUserId}' is disabled.");

        var role = await _userRepository.GetRoleAsync(appUserId);
        if (!string.Equals(role, "Viewer", StringComparison.OrdinalIgnoreCase))
            throw new InvalidOperationException(
                "Only Viewer accounts can be linked as tenant owners.");

        await _tenantRepository.SetAppUserIdAsync(tenantId, appUserId);
        return (await _tenantRepository.GetByIdAsync(tenantId))!;
    }

    public Task<LegalDocumentDto> AddDocumentAsync(CreateLegalDocumentDto dto) =>
        _tenantRepository.AddDocumentAsync(dto);

    public async Task<IEnumerable<LegalDocumentDto>> GetDocumentsAsync(int tenantId)
    {
        var ownedIds = await _ownershipResolver.GetOwnedTenantIdsAsync();
        DataScope.EnsureViewerOwnedTenantAccess(ownedIds, tenantId);
        return await _tenantRepository.GetDocumentsAsync(tenantId);
    }
}
