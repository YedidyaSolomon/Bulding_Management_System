using System.ComponentModel.DataAnnotations;

namespace BMS.Application.DTOs.Units;

public class UnitDto
{
    public int     Id                  { get; set; }
    public int     FloorNumber         { get; set; }
    public string  UnitNumber          { get; set; } = string.Empty;
    public string  UnitType            { get; set; } = string.Empty;
    public decimal AreaSqMeters        { get; set; }
    public decimal MonthlyRent         { get; set; }
    public string  Status              { get; set; } = string.Empty;
    public string? Description         { get; set; }
    /// <summary>
    /// Populated only in the selectable-for-lease response.
    /// The ID of the tenant this unit is currently reserved for (null otherwise).
    /// </summary>
    public int?    ReservedForTenantId { get; set; }
    /// <summary>
    /// Populated only in the selectable-for-lease response.
    /// True when this unit is Reserved and the reservation matches the queried tenant.
    /// The frontend uses this flag to pin the unit at the top with a "Reserved for you" label.
    /// </summary>
    public bool    IsReservedForRequestedTenant { get; set; }
}

public class CreateUnitDto
{
    [Range(1, 7, ErrorMessage = "Floor number must be between 1 and 7.")]
    public int FloorNumber { get; set; }

    [Required, MaxLength(20)]
    public string  UnitNumber   { get; set; } = string.Empty;

    [Required]
    public string  UnitType     { get; set; } = string.Empty;

    [Range(1, double.MaxValue, ErrorMessage = "Area must be at least 1 sq. meter.")]
    public decimal AreaSqMeters { get; set; }

    [Range(0.01, double.MaxValue, ErrorMessage = "Monthly rent must be greater than 0.")]
    public decimal MonthlyRent  { get; set; }

    public string? Description  { get; set; }
}

public class UpdateUnitDto
{
    [Range(1, 7, ErrorMessage = "Floor number must be between 1 and 7.")]
    public int FloorNumber { get; set; }

    [Required, MaxLength(20)]
    public string  UnitNumber   { get; set; } = string.Empty;

    [Required]
    public string  UnitType     { get; set; } = string.Empty;

    [Range(1, double.MaxValue, ErrorMessage = "Area must be at least 1 sq. meter.")]
    public decimal AreaSqMeters { get; set; }

    [Range(0.01, double.MaxValue, ErrorMessage = "Monthly rent must be greater than 0.")]
    public decimal MonthlyRent  { get; set; }

    [Required]
    public string  Status       { get; set; } = string.Empty;

    public string? Description  { get; set; }
}

public class ReserveUnitDto
{
    /// <summary>The tenant for whom this unit is being reserved.</summary>
    [Range(1, int.MaxValue, ErrorMessage = "TenantId must be a positive integer.")]
    public int TenantId { get; set; }
}
