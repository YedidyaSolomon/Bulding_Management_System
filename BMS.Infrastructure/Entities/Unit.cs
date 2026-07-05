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

    public DateTime  CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? UpdatedAt { get; set; }

    public Floor?             Floor  { get; set; }
    public ICollection<Lease> Leases { get; set; } = new List<Lease>();
}
