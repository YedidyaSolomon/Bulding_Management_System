using System.ComponentModel.DataAnnotations;

namespace BMS.Application.DTOs.Tenants;

public class TenantDto
{
    public int     Id                { get; set; }
    /// <summary>AppUser.Id of the linked user account. Null if not yet linked.</summary>
    public string? AppUserId         { get; set; }
    /// <summary>Email of the linked user account — convenience display field.</summary>
    public string? UserEmail         { get; set; }
    public string  OrganizationName  { get; set; } = string.Empty;
    public string  TIN               { get; set; } = string.Empty;
    public string  Phone             { get; set; } = string.Empty;
    public string  BusinessType      { get; set; } = string.Empty;
    public string  ContactPersonName { get; set; } = string.Empty;
    public string  ContactEmail      { get; set; } = string.Empty;
    public bool    IsActive          { get; set; }
}

public class CreateTenantDto
{
    /// <summary>
    /// Optional: AppUser.Id of a registered Viewer account to link as the tenant owner.
    /// Admin selects the user from a list/search (e.g. by email) in the UI.
    /// If omitted the tenant is created unlinked and can be linked later via
    /// PUT /api/tenants/{id}/link-user.
    /// </summary>
    [MaxLength(450)]
    public string? AppUserId         { get; set; }

    [Required, MaxLength(150)]
    public string  OrganizationName  { get; set; } = string.Empty;

    [Required, MaxLength(50)]
    public string  TIN               { get; set; } = string.Empty;

    [Required, MaxLength(20)]
    public string  Phone             { get; set; } = string.Empty;

    [Required]
    public string  BusinessType      { get; set; } = string.Empty;

    [Required, MaxLength(100)]
    public string  ContactPersonName { get; set; } = string.Empty;

    [Required, EmailAddress, MaxLength(150)]
    public string  ContactEmail      { get; set; } = string.Empty;
}

public class UpdateTenantDto
{
    [Required, MaxLength(150)]
    public string OrganizationName  { get; set; } = string.Empty;

    [Required, MaxLength(50)]
    public string TIN               { get; set; } = string.Empty;

    [Required, MaxLength(20)]
    public string Phone             { get; set; } = string.Empty;

    [Required]
    public string BusinessType      { get; set; } = string.Empty;

    [Required, MaxLength(100)]
    public string ContactPersonName { get; set; } = string.Empty;

    [Required, EmailAddress, MaxLength(150)]
    public string ContactEmail      { get; set; } = string.Empty;

    public bool IsActive { get; set; }
}

/// <summary>Request body for PUT /api/tenants/{id}/link-user.</summary>
public class LinkTenantUserDto
{
    [Required]
    public string AppUserId { get; set; } = string.Empty;

    /// <summary>
    /// Set to <c>true</c> to overwrite an existing non-null AppUserId.
    /// Defaults to <c>false</c> so accidental overwrites are blocked.
    /// </summary>
    public bool Force { get; set; } = false;
}

public class LegalDocumentDto
{
    public int       Id           { get; set; }
    public int       TenantId     { get; set; }
    public string    DocumentType { get; set; } = string.Empty;
    public string    FilePath     { get; set; } = string.Empty;
    public DateTime  UploadedAt   { get; set; }
    public DateTime? ExpiryDate   { get; set; }
    public bool      IsVerified   { get; set; }
}

public class CreateLegalDocumentDto
{
    public int       TenantId     { get; set; }
    public string    DocumentType { get; set; } = string.Empty;
    public string    FilePath     { get; set; } = string.Empty;
    public DateTime? ExpiryDate   { get; set; }
}
