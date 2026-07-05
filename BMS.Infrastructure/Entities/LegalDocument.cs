using BMS.Infrastructure.Enums;

namespace BMS.Infrastructure.Entities;

public class LegalDocument
{
    public int          Id           { get; set; }
    public int          TenantId     { get; set; }
    public DocumentType DocumentType { get; set; }
    public string       FilePath     { get; set; } = string.Empty;
    public DateTime     UploadedAt   { get; set; } = DateTime.UtcNow;
    public DateTime?    ExpiryDate   { get; set; }
    public bool         IsVerified   { get; set; } = false;

    public Tenant Tenant { get; set; } = null!;
}
