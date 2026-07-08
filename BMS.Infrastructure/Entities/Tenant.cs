using BMS.Infrastructure.Enums;

namespace BMS.Infrastructure.Entities;

public class Tenant
{
    public int Id { get; set; }

    /// <summary>
    /// Links this tenant (business) to the registered AppUser account.
    /// Nullable — a Tenant can be created by Admin before the user registers,
    /// and linked later via PUT /api/tenants/{id}/link-user.
    /// One user can own many Tenants (one-to-many; FK lives here, not on AppUser).
    /// </summary>
    public string? AppUserId { get; set; }

    public string OrganizationName  { get; set; } = string.Empty;
    public string TIN               { get; set; } = string.Empty;
    public string Phone             { get; set; } = string.Empty;
    public BusinessType BusinessType { get; set; }
    public string ContactPersonName { get; set; } = string.Empty;
    public string ContactEmail      { get; set; } = string.Empty;
    public bool   IsActive          { get; set; } = true;

    public DateTime  CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? UpdatedAt { get; set; }

    /// <summary>Navigation to the owning user account. Null if not yet linked.</summary>
    public AppUser? AppUser { get; set; }

    public ICollection<Lease>         Leases         { get; set; } = new List<Lease>();
    public ICollection<LegalDocument> LegalDocuments { get; set; } = new List<LegalDocument>();
}
