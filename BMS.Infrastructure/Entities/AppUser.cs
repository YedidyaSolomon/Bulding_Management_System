using Microsoft.AspNetCore.Identity;

namespace BMS.Infrastructure.Entities;

/// <summary>
/// Extends ASP.NET Core IdentityUser with BMS-specific fields.
/// IdentityUser already provides: Id, Email, UserName, PasswordHash,
/// SecurityStamp, ConcurrencyStamp, PhoneNumber, etc.
///
/// We add:
///   FullName  — displayed in the UI and included in the JWT claim
///   IsActive  — soft-disable a user without deleting them
///   Audit fields — required by the assignment for all entities
///
/// One AppUser can own many Tenants (one-to-many).
/// The FK lives on Tenant.AppUserId, NOT here.
/// </summary>
public class AppUser : IdentityUser
{
    public string FullName { get; set; } = string.Empty;
    public bool IsActive { get; set; } = true;

    // Audit fields
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? UpdatedAt { get; set; }

    /// <summary>
    /// All tenant businesses this user owns.
    /// A Viewer can own multiple tenants; Admin/Manager accounts will have none.
    /// </summary>
    public ICollection<Tenant> Tenants { get; set; } = new List<Tenant>();

    public ICollection<Notification> Notifications { get; set; } = new List<Notification>();
}
