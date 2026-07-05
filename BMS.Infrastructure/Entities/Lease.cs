using BMS.Infrastructure.Enums;

namespace BMS.Infrastructure.Entities;

public class Lease
{
    public int         Id                 { get; set; }
    public int         UnitId             { get; set; }
    public int         TenantId           { get; set; }
    public DateTime    StartDate          { get; set; }
    public DateTime    EndDate            { get; set; }
    public decimal     MonthlyRent        { get; set; }
    public decimal     DepositAmount      { get; set; }
    public LeaseStatus Status             { get; set; } = LeaseStatus.Active;
    public string?     TerminationReason  { get; set; }

    public DateTime  CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? UpdatedAt { get; set; }

    public Unit   Unit   { get; set; } = null!;
    public Tenant Tenant { get; set; } = null!;

    public ICollection<Invoice> Invoices { get; set; } = new List<Invoice>();
}
