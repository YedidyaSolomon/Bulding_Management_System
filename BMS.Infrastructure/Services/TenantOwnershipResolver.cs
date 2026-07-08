using BMS.Application.Interfaces;
using BMS.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace BMS.Infrastructure.Services;

/// <summary>
/// Queries the database to resolve which Tenant IDs the current HTTP-request
/// user owns.  Registered as <b>Scoped</b> — one instance per request.
/// </summary>
public class TenantOwnershipResolver : ITenantOwnershipResolver
{
    private readonly ICurrentUserService    _currentUser;
    private readonly ApplicationDbContext   _context;

    public TenantOwnershipResolver(
        ICurrentUserService  currentUser,
        ApplicationDbContext context)
    {
        _currentUser = currentUser;
        _context     = context;
    }

    /// <inheritdoc/>
    public async Task<List<int>?> GetOwnedTenantIdsAsync()
    {
        // Admin and Manager bypass all tenant filtering — signal with null.
        if (_currentUser.IsAdminOrManager)
            return null;

        // Viewer (or any other role): return only the tenant IDs they own.
        var userId = _currentUser.UserId;
        if (string.IsNullOrEmpty(userId))
            return new List<int>(); // no identity → empty scope

        return await _context.Tenants
            .Where(t => t.AppUserId == userId)
            .Select(t => t.Id)
            .ToListAsync();
    }
}
