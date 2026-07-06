namespace BMS.Application.DTOs.Leases;

public class LeaseDto
{
    public int      Id                { get; set; }
    public int      UnitId            { get; set; }
    public string   UnitNumber        { get; set; } = string.Empty;
    public int      TenantId          { get; set; }
    public string   TenantName        { get; set; } = string.Empty;
    public DateTime StartDate         { get; set; }
    public DateTime EndDate           { get; set; }
    public decimal  MonthlyRent       { get; set; }
    public decimal  DepositAmount     { get; set; }
    public string   Status            { get; set; } = string.Empty;
    public string?  TerminationReason { get; set; }
}

public class CreateLeaseDto
{
    public int      UnitId        { get; set; }
    public int      TenantId      { get; set; }
    public DateTime StartDate     { get; set; }
    public DateTime EndDate       { get; set; }
    public decimal  MonthlyRent   { get; set; }
    public decimal  DepositAmount { get; set; }
}

public class UpdateLeaseDto
{
    public DateTime EndDate       { get; set; }
    public decimal  MonthlyRent   { get; set; }
    public decimal  DepositAmount { get; set; }
    public string   Status        { get; set; } = string.Empty;
}

public class TerminateLeaseDto
{
    public string Reason { get; set; } = string.Empty;
}

/// <summary>Lightweight projection used by the notification generator.</summary>
public class ExpiringLeaseDto
{
    public int      Id         { get; set; }
    public string   TenantName { get; set; } = string.Empty;
    public string   UnitNumber { get; set; } = string.Empty;
    /// <summary>The AppUser.Id of the tenant linked to this lease.</summary>
    public string   UserId     { get; set; } = string.Empty;
    public DateTime EndDate    { get; set; }
}
