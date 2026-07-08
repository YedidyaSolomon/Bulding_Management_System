using BMS.Infrastructure.Enums;

namespace BMS.Infrastructure.Entities;

public class Unit
{
    public int        Id           { get; set; }
    public int?       FloorId      { get; set; }
    public int        FloorNumber  { get; set; }
    public string     UnitNumber   { get; set; } = string.Empty;
    public UnitType   UnitType     { get; set; }
    public decimal    AreaSqMeters { get; set; }
    public decimal    MonthlyRent  { get; set; }
    public UnitStatus Status       { get; set; } = UnitStatus.Available;
    public string?    Description  { get; set; }

    /// <summary>
    /// When <see cref="Status"/> is <see cref="UnitStatus.Reserved"/>, this records
    /// which tenant the unit is being held for.  Cleared automatically when a Lease
    /// is created for this unit (status transitions to Occupied).
    /// Null for Available / Occupied / UnderMaintenance units.
    /// </summary>
    public int? ReservedForTenantId { get; set; }

    public DateTime  CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? UpdatedAt { get; set; }

    public Floor?             Floor             { get; set; }
    public Tenant?            ReservedForTenant { get; set; }
    public ICollection<Lease> Leases            { get; set; } = new List<Lease>();
}

