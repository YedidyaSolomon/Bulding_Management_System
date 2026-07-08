using BMS.Application.Interfaces;
using Moq;

namespace BMS.Tests.Helpers;

/// <summary>
/// Factory for pre-configured ITenantOwnershipResolver mocks.
///
///   null  — Admin/Manager (bypass all tenant filtering)
///   list  — Viewer with those specific tenant IDs in scope
///   empty — Viewer with no linked tenants (should see nothing)
/// </summary>
public static class OwnershipResolverFactory
{
    /// <summary>Returns null — Admin/Manager bypass (see everything).</summary>
    public static ITenantOwnershipResolver Bypass()
    {
        var mock = new Mock<ITenantOwnershipResolver>();
        mock.Setup(r => r.GetOwnedTenantIdsAsync())
            .ReturnsAsync((List<int>?)null);
        return mock.Object;
    }

    /// <summary>Returns the specified list of tenant IDs (Viewer scope).</summary>
    public static ITenantOwnershipResolver ForTenants(params int[] tenantIds)
    {
        var mock = new Mock<ITenantOwnershipResolver>();
        mock.Setup(r => r.GetOwnedTenantIdsAsync())
            .ReturnsAsync(tenantIds.ToList());
        return mock.Object;
    }

    /// <summary>Returns an empty list — Viewer with no linked tenants.</summary>
    public static ITenantOwnershipResolver Empty()
    {
        var mock = new Mock<ITenantOwnershipResolver>();
        mock.Setup(r => r.GetOwnedTenantIdsAsync())
            .ReturnsAsync(new List<int>());
        return mock.Object;
    }
}
