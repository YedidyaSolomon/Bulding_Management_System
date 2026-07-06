using BMS.Application.Common;
using BMS.Tests.Helpers;
using Xunit;

namespace BMS.Tests.Common;

/// <summary>
/// Unit tests for the DataScope static utility.
/// These tests have no dependencies — no mocks, no infrastructure.
/// </summary>
public class DataScopeTests
{
    // ── EnsureViewerTenantAccess ──────────────────────────────────────────────

    [Fact]
    public void EnsureViewerTenantAccess_Viewer_MatchingTenant_DoesNotThrow()
    {
        var user = CurrentUserFactory.Viewer(tenantId: 5);

        // Should complete without throwing
        DataScope.EnsureViewerTenantAccess(user, resourceTenantId: 5);
    }

    [Fact]
    public void EnsureViewerTenantAccess_Viewer_DifferentTenant_ThrowsKeyNotFoundException()
    {
        var user = CurrentUserFactory.Viewer(tenantId: 5);

        var ex = Assert.Throws<KeyNotFoundException>(
            () => DataScope.EnsureViewerTenantAccess(user, resourceTenantId: 99));

        Assert.Contains("not found", ex.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void EnsureViewerTenantAccess_Admin_AnyTenant_DoesNotThrow()
    {
        var user = CurrentUserFactory.Admin();

        // Admin is never blocked — must pass for any tenant ID
        DataScope.EnsureViewerTenantAccess(user, resourceTenantId: 99);
    }

    [Fact]
    public void EnsureViewerTenantAccess_Manager_AnyTenant_DoesNotThrow()
    {
        var user = CurrentUserFactory.Manager();

        DataScope.EnsureViewerTenantAccess(user, resourceTenantId: 99);
    }

    [Fact]
    public void EnsureViewerTenantAccess_ViewerWithNoTenant_ThrowsKeyNotFoundException()
    {
        var user = CurrentUserFactory.ViewerWithNoTenant();

        Assert.Throws<KeyNotFoundException>(
            () => DataScope.EnsureViewerTenantAccess(user, resourceTenantId: 5));
    }

    // ── EnsureViewerUserAccess ────────────────────────────────────────────────

    [Fact]
    public void EnsureViewerUserAccess_Viewer_OwnUserId_DoesNotThrow()
    {
        const string userId = "viewer-42";
        var user = CurrentUserFactory.Viewer(userId: userId);

        DataScope.EnsureViewerUserAccess(user, resourceUserId: userId);
    }

    [Fact]
    public void EnsureViewerUserAccess_Viewer_DifferentUserId_ThrowsKeyNotFoundException()
    {
        var user = CurrentUserFactory.Viewer(userId: "viewer-1");

        Assert.Throws<KeyNotFoundException>(
            () => DataScope.EnsureViewerUserAccess(user, resourceUserId: "other-user-99"));
    }

    [Fact]
    public void EnsureViewerUserAccess_Admin_AnyUserId_DoesNotThrow()
    {
        var user = CurrentUserFactory.Admin();

        // Admin is never blocked
        DataScope.EnsureViewerUserAccess(user, resourceUserId: "some-other-user");
    }

    // ── ResolveUserId ─────────────────────────────────────────────────────────

    [Fact]
    public void ResolveUserId_Viewer_AlwaysReturnsOwnUserId()
    {
        const string viewerId    = "viewer-7";
        const string requestedId = "admin-99";   // frontend tried to request a different user
        var user = CurrentUserFactory.Viewer(userId: viewerId);

        var resolved = DataScope.ResolveUserId(user, requestedId);

        // Viewer must always be redirected to their own ID
        Assert.Equal(viewerId, resolved);
        Assert.NotEqual(requestedId, resolved);
    }

    [Fact]
    public void ResolveUserId_Admin_ReturnsRequestedUserId()
    {
        const string requestedId = "some-tenant-user-5";
        var user = CurrentUserFactory.Admin();

        var resolved = DataScope.ResolveUserId(user, requestedId);

        // Admin can query any user
        Assert.Equal(requestedId, resolved);
    }

    [Fact]
    public void ResolveUserId_Manager_ReturnsRequestedUserId()
    {
        const string requestedId = "tenant-user-3";
        var user = CurrentUserFactory.Manager();

        var resolved = DataScope.ResolveUserId(user, requestedId);

        Assert.Equal(requestedId, resolved);
    }

    [Fact]
    public void ResolveUserId_Viewer_NullOwnUserId_ThrowsUnauthorizedException()
    {
        // Edge case: token has no sub claim — should never happen in production
        // but the utility must handle it gracefully.
        var user = CurrentUserFactory.ViewerWithNoTenant();
        // ViewerWithNoTenant sets UserId = "viewer-orphan" so it's not null.
        // Test the null-UserId path by calling with a truly empty-userId mock:
        var mock = new Moq.Mock<BMS.Application.Interfaces.ICurrentUserService>();
        mock.Setup(s => s.UserId).Returns((string?)null);
        mock.Setup(s => s.IsViewer).Returns(true);

        Assert.Throws<UnauthorizedAccessException>(
            () => DataScope.ResolveUserId(mock.Object, "whatever"));
    }
}
