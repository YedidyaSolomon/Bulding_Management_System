using BMS.Application.Interfaces;
using Moq;

namespace BMS.Tests.Helpers;

/// <summary>
/// Factory that produces pre-configured ICurrentUserService mocks so each
/// test class can write:
///
///   var user = CurrentUserFactory.Viewer(userId: "u1", tenantId: 10);
///   var user = CurrentUserFactory.Admin();
///   var user = CurrentUserFactory.Manager();
/// </summary>
public static class CurrentUserFactory
{
    // ── Viewer ────────────────────────────────────────────────────────────────

    public static ICurrentUserService Viewer(string userId = "viewer-1", int tenantId = 1)
    {
        var mock = new Mock<ICurrentUserService>();
        mock.Setup(s => s.UserId).Returns(userId);
        mock.Setup(s => s.Role).Returns("Viewer");
        mock.Setup(s => s.TenantId).Returns(tenantId);
        mock.Setup(s => s.IsViewer).Returns(true);
        mock.Setup(s => s.IsAdminOrManager).Returns(false);
        return mock.Object;
    }

    /// <summary>Viewer with no linked tenant — e.g. account registered but not yet onboarded.</summary>
    public static ICurrentUserService ViewerWithNoTenant(string userId = "viewer-orphan")
    {
        var mock = new Mock<ICurrentUserService>();
        mock.Setup(s => s.UserId).Returns(userId);
        mock.Setup(s => s.Role).Returns("Viewer");
        mock.Setup(s => s.TenantId).Returns((int?)null);
        mock.Setup(s => s.IsViewer).Returns(true);
        mock.Setup(s => s.IsAdminOrManager).Returns(false);
        return mock.Object;
    }

    // ── Admin ─────────────────────────────────────────────────────────────────

    public static ICurrentUserService Admin(string userId = "admin-1")
    {
        var mock = new Mock<ICurrentUserService>();
        mock.Setup(s => s.UserId).Returns(userId);
        mock.Setup(s => s.Role).Returns("Admin");
        mock.Setup(s => s.TenantId).Returns((int?)null);
        mock.Setup(s => s.IsViewer).Returns(false);
        mock.Setup(s => s.IsAdminOrManager).Returns(true);
        return mock.Object;
    }

    // ── Manager ───────────────────────────────────────────────────────────────

    public static ICurrentUserService Manager(string userId = "manager-1")
    {
        var mock = new Mock<ICurrentUserService>();
        mock.Setup(s => s.UserId).Returns(userId);
        mock.Setup(s => s.Role).Returns("Manager");
        mock.Setup(s => s.TenantId).Returns((int?)null);
        mock.Setup(s => s.IsViewer).Returns(false);
        mock.Setup(s => s.IsAdminOrManager).Returns(true);
        return mock.Object;
    }
}
