using System.ComponentModel.DataAnnotations;

namespace BMS.Application.DTOs.Tenants;

public class TenantDto
{
    public int    Id                { get; set; }
    /// <summary>The AppUser.Id this tenant belongs to.</summary>
    public string UserId            { get; set; } = string.Empty;
    /// <summary>Email of the linked user account — convenience display field.</summary>
    public string? UserEmail         { get; set; }
    public string OrganizationName  { get; set; } = string.Empty;
    public string TIN               { get; set; } = string.Empty;
    public string Phone             { get; set; } = string.Empty;
    public string BusinessType      { get; set; } = string.Empty;
    public string ContactPersonName { get; set; } = string.Empty;
    public string ContactEmail      { get; set; } = string.Empty;
    public bool   IsActive          { get; set; }
}

public class CreateTenantDto
{
    /// <summary>
    /// Email of an already-registered user account.
    /// The backend resolves this to a UserId.
    /// </summary>
    [Required, EmailAddress, MaxLength(150)]
    public string UserEmail         { get; set; } = string.Empty;

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

    public bool   IsActive          { get; set; }
}

public class LegalDocumentDto
{
    public int      Id           { get; set; }
    public int      TenantId     { get; set; }
    public string   DocumentType { get; set; } = string.Empty;
    public string   FilePath     { get; set; } = string.Empty;
    public DateTime UploadedAt   { get; set; }
    public DateTime? ExpiryDate  { get; set; }
    public bool     IsVerified   { get; set; }
}

public class CreateLegalDocumentDto
{
    public int      TenantId     { get; set; }
    public string   DocumentType { get; set; } = string.Empty;
    public string   FilePath     { get; set; } = string.Empty;
    public DateTime? ExpiryDate  { get; set; }
}
