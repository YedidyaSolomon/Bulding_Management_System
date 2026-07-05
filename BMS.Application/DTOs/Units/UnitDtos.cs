namespace BMS.Application.DTOs.Units;

public class UnitDto
{
    public int     Id           { get; set; }
    public int     FloorNumber  { get; set; }
    public string  UnitNumber   { get; set; } = string.Empty;
    public string  UnitType     { get; set; } = string.Empty;
    public decimal AreaSqMeters { get; set; }
    public decimal MonthlyRent  { get; set; }
    public string  Status       { get; set; } = string.Empty;
    public string? Description  { get; set; }
}

public class CreateUnitDto
{
    public int     FloorNumber  { get; set; }
    public string  UnitNumber   { get; set; } = string.Empty;
    public string  UnitType     { get; set; } = string.Empty;
    public decimal AreaSqMeters { get; set; }
    public decimal MonthlyRent  { get; set; }
    public string? Description  { get; set; }
}

public class UpdateUnitDto
{
    public int     FloorNumber  { get; set; }
    public string  UnitNumber   { get; set; } = string.Empty;
    public string  UnitType     { get; set; } = string.Empty;
    public decimal AreaSqMeters { get; set; }
    public decimal MonthlyRent  { get; set; }
    public string  Status       { get; set; } = string.Empty;
    public string? Description  { get; set; }
}
