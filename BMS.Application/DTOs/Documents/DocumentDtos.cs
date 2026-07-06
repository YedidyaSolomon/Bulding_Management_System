namespace BMS.Application.DTOs.Documents;

/// <summary>Lightweight projection for expiring documents used by the notification generator.</summary>
public class ExpiringDocumentDto
{
    public int      Id           { get; set; }
    public int      TenantId     { get; set; }
    public string   TenantName   { get; set; } = string.Empty;
    /// <summary>The AppUser.Id of the tenant who owns this document.</summary>
    public string   UserId       { get; set; } = string.Empty;
    public string   DocumentType { get; set; } = string.Empty;
    public DateTime ExpiryDate   { get; set; }
}
