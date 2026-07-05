namespace BMS.Infrastructure.Entities;

public class Floor
{
    public int    Id          { get; set; }
    public int    BuildingId  { get; set; }
    public int    FloorNumber { get; set; }
    public string Label       { get; set; } = string.Empty;

    public DateTime  CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? UpdatedAt { get; set; }

    public Building          Building { get; set; } = null!;
    public ICollection<Unit> Units    { get; set; } = new List<Unit>();
}
