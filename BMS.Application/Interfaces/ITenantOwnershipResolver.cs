namespace BMS.Application.Interfaces;

/// <summary>
/// Resolves which Tenant IDs the current user is allowed to see.
///
/// Return semantics:
///   null  — caller is Admin or Manager; skip all tenant filtering (see everything).
///   list  — caller is a Viewer; the list contains every Tenant.Id where
///            Tenant.AppUserId == currentUser.UserId. May be empty if the
///            Viewer account has not been linked to any tenant yet.
/// </summary>
public interface ITenantOwnershipResolver
{
    Task<List<int>?> GetOwnedTenantIdsAsync();
}
