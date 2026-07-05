namespace BMS.Application.DTOs.Dashboard;

public class DashboardSummaryDto
{
    public int     TotalUnits          { get; set; }
    public int     OccupiedUnits       { get; set; }
    public int     AvailableUnits      { get; set; }
    public int     TotalTenants        { get; set; }
    public int     ActiveLeases        { get; set; }
    public int     ExpiringLeasesIn30Days { get; set; }
    public decimal TotalMonthlyRevenue { get; set; }
    public decimal OutstandingAmount   { get; set; }
    public int     OverdueInvoices     { get; set; }
    public int     UnreadNotifications { get; set; }
}
