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
/// </summary>
public class AppUser : IdentityUser
{
    public string FullName { get; set; } = string.Empty;
    public bool IsActive { get; set; } = true;

    // Audit fields (populated by DbContext SaveChanges override in future)
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? UpdatedAt { get; set; }

    public ICollection<Notification> Notifications { get; set; }
       = new List<Notification>();
}
