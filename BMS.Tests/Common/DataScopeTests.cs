using BMS.Application.Common;
using BMS.Application.Exceptions;
using BMS.Tests.Helpers;
using Xunit;

namespace BMS.Tests.Common;

/// <summary>
/// Unit tests for the DataScope static utility.
/// No mocks, no infrastructure — pure logic only.
///
/// EnsureViewerOwnedTenantAccess semantics:
///   ownedIds == null  → bypass (Admin/Manager) — never throws
///   ownedIds contains resourceTenantId → allowed — never throws
///   ownedIds does NOT contain resourceTenantId → throws ForbiddenAccessException
/// </summary>
public class DataScopeTests
{
    // ── EnsureViewerOwnedTenantAccess ─────────────────────────────────────────

    [Fact]
    public void EnsureViewerOwnedTenantAccess_NullList_Bypass_DoesNotThrow()
    {
        // null = Admin/Manager signal — must never throw
        DataScope.EnsureViewerOwnedTenantAccess(null, resourceTenantId: 99);
    }

    [Fact]
    public void EnsureViewerOwnedTenantAccess_ListContainsTenant_DoesNotThrow()
    {
        var ownedIds = new List<int> { 5, 10, 15 };

        DataScope.EnsureViewerOwnedTenantAccess(ownedIds, resourceTenantId: 10);
    }

    [Fact]
    public void EnsureViewerOwnedTenantAccess_ListDoesNotContainTenant_ThrowsForbidden()
    {
        var ownedIds = new List<int> { 5, 10 };

        Assert.Throws<ForbiddenAccessException>(
            () => DataScope.EnsureViewerOwnedTenantAccess(ownedIds, resourceTenantId: 99));
    }

    [Fact]
    public void EnsureViewerOwnedTenantAccess_EmptyList_ThrowsForbidden()
    {
        var ownedIds = new List<int>();

        Assert.Throws<ForbiddenAccessException>(
            () => DataScope.EnsureViewerOwnedTenantAccess(ownedIds, resourceTenantId: 5));
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

        DataScope.EnsureViewerUserAccess(user, resourceUserId: "some-other-user");
    }

    // ── ResolveUserId ─────────────────────────────────────────────────────────

    [Fact]
    public void ResolveUserId_Viewer_AlwaysReturnsOwnUserId()
    {
        const string viewerId    = "viewer-7";
        const string requestedId = "admin-99";
        var user = CurrentUserFactory.Viewer(userId: viewerId);

        var resolved = DataScope.ResolveUserId(user, requestedId);

        Assert.Equal(viewerId, resolved);
        Assert.NotEqual(requestedId, resolved);
    }

    [Fact]
    public void ResolveUserId_Admin_ReturnsRequestedUserId()
    {
        const string requestedId = "some-tenant-user-5";
        var user = CurrentUserFactory.Admin();

        var resolved = DataScope.ResolveUserId(user, requestedId);

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
        var mock = new Moq.Mock<BMS.Application.Interfaces.ICurrentUserService>();
        mock.Setup(s => s.UserId).Returns((string?)null);
        mock.Setup(s => s.IsViewer).Returns(true);

        Assert.Throws<UnauthorizedAccessException>(
            () => DataScope.ResolveUserId(mock.Object, "whatever"));
    }
}
