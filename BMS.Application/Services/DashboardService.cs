using BMS.Application.DTOs.Dashboard;
using BMS.Application.DTOs.Tenants;
using BMS.Application.Interfaces;
using BMS.Application.Interfaces.Repositories;

namespace BMS.Application.Services;

public class DashboardService : IDashboardService
{
    private readonly IUnitRepository          _unitRepository;
    private readonly ITenantRepository        _tenantRepository;
    private readonly ILeaseRepository         _leaseRepository;
    private readonly IInvoiceRepository       _invoiceRepository;
    private readonly INotificationRepository  _notificationRepository;
    private readonly ICurrentUserService      _currentUser;
    private readonly ITenantOwnershipResolver _ownershipResolver;

    public DashboardService(
        IUnitRepository          unitRepository,
        ITenantRepository        tenantRepository,
        ILeaseRepository         leaseRepository,
        IInvoiceRepository       invoiceRepository,
        INotificationRepository  notificationRepository,
        ICurrentUserService      currentUser,
        ITenantOwnershipResolver ownershipResolver)
    {
        _unitRepository         = unitRepository;
        _tenantRepository       = tenantRepository;
        _leaseRepository        = leaseRepository;
        _invoiceRepository      = invoiceRepository;
        _notificationRepository = notificationRepository;
        _currentUser            = currentUser;
        _ownershipResolver      = ownershipResolver;
    }

    public async Task<DashboardSummaryDto> GetSummaryAsync(string userId)
    {
        var resolvedUserId = _currentUser.IsViewer
            ? (_currentUser.UserId ?? userId)
            : userId;

        // Resolve owned tenant IDs once for the whole request.
        // null  → Admin/Manager (bypass, see everything)
        // list  → Viewer scope (may be empty if not yet linked to any tenant)
        var ownedIds = await _ownershipResolver.GetOwnedTenantIdsAsync();

        var units = (await _unitRepository.GetAllAsync()).ToList();

        List<TenantDto> tenants;
        if (ownedIds is null)
            tenants = (await _tenantRepository.GetAllAsync()).ToList();
        else if (ownedIds.Count == 0)
            tenants = new List<TenantDto>();
        else
            tenants = (await _tenantRepository.GetByIdsAsync(ownedIds)).ToList();

        List<DTOs.Leases.LeaseDto> leases;
        if (ownedIds is null)
        {
            leases = (await _leaseRepository.GetAllAsync()).ToList();
        }
        else if (ownedIds.Count == 0)
        {
            leases = new List<DTOs.Leases.LeaseDto>();
        }
        else
        {
            var leaseTasks = ownedIds.Select(tid => _leaseRepository.GetByTenantIdAsync(tid));
            var leaseResults = await Task.WhenAll(leaseTasks);
            leases = leaseResults.SelectMany(x => x).ToList();
        }

        List<DTOs.Invoices.InvoiceDto> invoices;
        if (ownedIds is null)
        {
            invoices = (await _invoiceRepository.GetAllAsync()).ToList();
        }
        else if (ownedIds.Count == 0)
        {
            invoices = new List<DTOs.Invoices.InvoiceDto>();
        }
        else
        {
            var invoiceTasks = ownedIds.Select(tid => _invoiceRepository.GetByTenantIdAsync(tid));
            var invoiceResults = await Task.WhenAll(invoiceTasks);
            invoices = invoiceResults.SelectMany(x => x).ToList();
        }

        var overdue = (await _invoiceRepository.GetOverdueAsync()).ToList();
        if (ownedIds is not null)
        {
            var ownedInvoiceIds = invoices.Select(i => i.Id).ToHashSet();
            overdue = overdue.Where(i => ownedInvoiceIds.Contains(i.Id)).ToList();
        }

        var expiringLeases = leases.Count(l =>
            l.Status == "Active" &&
            l.EndDate <= DateTime.UtcNow.AddDays(30));

        return new DashboardSummaryDto
        {
            TotalUnits             = units.Count,
            OccupiedUnits          = units.Count(u => u.Status == "Occupied"),
            AvailableUnits         = units.Count(u => u.Status == "Available"),
            TotalTenants           = tenants.Count,
            ActiveLeases           = leases.Count(l => l.Status == "Active"),
            ExpiringLeasesIn30Days = expiringLeases,
            TotalMonthlyRevenue    = leases.Where(l => l.Status == "Active").Sum(l => l.MonthlyRent),
            OutstandingAmount      = invoices.Where(i => i.Status is "Issued" or "Overdue").Sum(i => i.AmountDue),
            OverdueInvoices        = overdue.Count,
            UnreadNotifications    = await _notificationRepository.GetUnreadCountAsync(resolvedUserId)
        };
    }
}
