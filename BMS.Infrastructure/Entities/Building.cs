namespace BMS.Infrastructure.Entities;

public class Building
{
    public int    Id          { get; set; }
    public string Name        { get; set; } = string.Empty;
    public string Address     { get; set; } = string.Empty;
    public int    TotalFloors { get; set; }

    public DateTime  CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? UpdatedAt { get; set; }

    public ICollection<Floor> Floors { get; set; } = new List<Floor>();
}
