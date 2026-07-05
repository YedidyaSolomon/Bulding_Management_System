namespace BMS.Application.DTOs.Tenants;

public class TenantDto
{
    public int    Id                { get; set; }
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
    public string OrganizationName  { get; set; } = string.Empty;
    public string TIN               { get; set; } = string.Empty;
    public string Phone             { get; set; } = string.Empty;
    public string BusinessType      { get; set; } = string.Empty;
    public string ContactPersonName { get; set; } = string.Empty;
    public string ContactEmail      { get; set; } = string.Empty;
}

public class UpdateTenantDto
{
    public string OrganizationName  { get; set; } = string.Empty;
    public string TIN               { get; set; } = string.Empty;
    public string Phone             { get; set; } = string.Empty;
    public string BusinessType      { get; set; } = string.Empty;
    public string ContactPersonName { get; set; } = string.Empty;
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
