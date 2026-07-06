using BMS.Application.DTOs.Dashboard;
using BMS.Application.Interfaces;
using BMS.Application.Interfaces.Repositories;

namespace BMS.Application.Services;

public class DashboardService : IDashboardService
{
    private readonly IUnitRepository         _unitRepository;
    private readonly ITenantRepository       _tenantRepository;
    private readonly ILeaseRepository        _leaseRepository;
    private readonly IInvoiceRepository      _invoiceRepository;
    private readonly INotificationRepository _notificationRepository;
    private readonly ICurrentUserService     _currentUser;

    public DashboardService(
        IUnitRepository unitRepository,
        ITenantRepository tenantRepository,
        ILeaseRepository leaseRepository,
        IInvoiceRepository invoiceRepository,
        INotificationRepository notificationRepository,
        ICurrentUserService currentUser)
    {
        _unitRepository         = unitRepository;
        _tenantRepository       = tenantRepository;
        _leaseRepository        = leaseRepository;
        _invoiceRepository      = invoiceRepository;
        _notificationRepository = notificationRepository;
        _currentUser            = currentUser;
    }

    public async Task<DashboardSummaryDto> GetSummaryAsync(string userId)
    {
        var resolvedUserId = userId;
        if (_currentUser.IsViewer)
            resolvedUserId = _currentUser.UserId ?? userId;

        var units    = (await _unitRepository.GetAllAsync()).ToList();
        var tenants  = _currentUser.IsViewer && _currentUser.TenantId.HasValue
            ? (await _tenantRepository.GetByIdAsync(_currentUser.TenantId.Value)) is { } t
                ? new List<DTOs.Tenants.TenantDto> { t }
                : new List<DTOs.Tenants.TenantDto>()
            : (await _tenantRepository.GetAllAsync()).ToList();

        var leases = _currentUser.IsViewer && _currentUser.TenantId.HasValue
            ? (await _leaseRepository.GetByTenantIdAsync(_currentUser.TenantId.Value)).ToList()
            : (await _leaseRepository.GetAllAsync()).ToList();

        var invoices = _currentUser.IsViewer && _currentUser.TenantId.HasValue
            ? (await _invoiceRepository.GetByTenantIdAsync(_currentUser.TenantId.Value)).ToList()
            : (await _invoiceRepository.GetAllAsync()).ToList();

        var overdue = (await _invoiceRepository.GetOverdueAsync()).ToList();
        if (_currentUser.IsViewer && _currentUser.TenantId.HasValue)
        {
            var tenantInvoiceIds = invoices.Select(i => i.Id).ToHashSet();
            overdue = overdue.Where(i => tenantInvoiceIds.Contains(i.Id)).ToList();
        }

        var expiringLeases = leases.Count(l =>
            l.Status == "Active" &&
            DateTime.TryParse(l.EndDate.ToString(), out var end) &&
            end <= DateTime.UtcNow.AddDays(30));

        return new DashboardSummaryDto
        {
            TotalUnits               = units.Count,
            OccupiedUnits            = units.Count(u => u.Status == "Occupied"),
            AvailableUnits           = units.Count(u => u.Status == "Available"),
            TotalTenants             = tenants.Count,
            ActiveLeases             = leases.Count(l => l.Status == "Active"),
            ExpiringLeasesIn30Days   = expiringLeases,
            TotalMonthlyRevenue      = leases.Where(l => l.Status == "Active").Sum(l => l.MonthlyRent),
            OutstandingAmount        = invoices.Where(i => i.Status is "Issued" or "Overdue").Sum(i => i.AmountDue),
            OverdueInvoices          = overdue.Count,
            UnreadNotifications      = await _notificationRepository.GetUnreadCountAsync(resolvedUserId)
        };
    }
}
