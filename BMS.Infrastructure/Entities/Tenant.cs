using BMS.Infrastructure.Enums;

namespace BMS.Infrastructure.Entities;

public class Tenant
{
    public int          Id                { get; set; }
    public string       OrganizationName  { get; set; } = string.Empty;
    public string       TIN               { get; set; } = string.Empty;
    public string       Phone             { get; set; } = string.Empty;
    public BusinessType BusinessType      { get; set; }
    public string       ContactPersonName { get; set; } = string.Empty;
    public string       ContactEmail      { get; set; } = string.Empty;
    public bool         IsActive          { get; set; } = true;

    public DateTime  CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? UpdatedAt { get; set; }

    public ICollection<Lease>         Leases         { get; set; } = new List<Lease>();
    public ICollection<LegalDocument> LegalDocuments { get; set; } = new List<LegalDocument>();
}
