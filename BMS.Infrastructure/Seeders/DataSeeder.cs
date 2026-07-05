using BMS.Infrastructure.Data;
using BMS.Infrastructure.Entities;
using BMS.Infrastructure.Enums;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace BMS.Infrastructure.Seeders;

/// <summary>
/// Seeds all required business data:
///   1 Building → 7 Floors → 3 Units per floor (21 total)
///   5 Tenants → 5 active Leases → Invoices → Payments → Notifications
///
/// Fully idempotent — safe to run on every startup.
/// </summary>
public static class DataSeeder
{
    public static async Task SeedAsync(IServiceProvider serviceProvider)
    {
        var context     = serviceProvider.GetRequiredService<ApplicationDbContext>();
        var userManager = serviceProvider.GetRequiredService<UserManager<AppUser>>();
        var logger      = serviceProvider.GetRequiredService<ILogger<ApplicationDbContext>>();

        // ── 1. Building ──────────────────────────────────────────────────────
        if (await context.Buildings.AnyAsync())
        {
            logger.LogInformation("DataSeeder: data already exists, skipping.");
            return;
        }

        logger.LogInformation("DataSeeder: seeding initial business data...");

        var building = new Building
        {
            Name        = "Horizon Business Tower",
            Address     = "123 Commerce Avenue, Downtown District",
            TotalFloors = 7,
            CreatedAt   = DateTime.UtcNow
        };
        context.Buildings.Add(building);
        await context.SaveChangesAsync();

        // ── 2. Floors ────────────────────────────────────────────────────────
        var floors = Enumerable.Range(1, 7).Select(n => new Floor
        {
            BuildingId  = building.Id,
            FloorNumber = n,
            Label       = $"Floor {n}",
            CreatedAt   = DateTime.UtcNow
        }).ToList();

        context.Floors.AddRange(floors);
        await context.SaveChangesAsync();

        // ── 3. Units (3 per floor = 21 total) ───────────────────────────────
        var unitDefs = new[]
        {
            (UnitType.Office, 45.0m,  1800m, "Standard office unit"),
            (UnitType.Office, 65.0m,  2500m, "Large corner office"),
            (UnitType.Shop,   30.0m,  1200m, "Retail shop unit")
        };

        var allUnits = new List<Unit>();
        foreach (var floor in floors)
        {
            for (int i = 0; i < unitDefs.Length; i++)
            {
                var (type, area, rent, desc) = unitDefs[i];
                allUnits.Add(new Unit
                {
                    FloorId      = floor.Id,
                    FloorNumber  = floor.FloorNumber,
                    UnitNumber   = $"{floor.FloorNumber}0{i + 1}",
                    UnitType     = type,
                    AreaSqMeters = area,
                    MonthlyRent  = rent,
                    Status       = UnitStatus.Available,
                    Description  = desc,
                    CreatedAt    = DateTime.UtcNow
                });
            }
        }

        context.Units.AddRange(allUnits);
        await context.SaveChangesAsync();

        // ── 4. Tenants ───────────────────────────────────────────────────────
        var tenantData = new[]
        {
            ("Apex Solutions Ltd",       "TIN-001-2024", "+251911000001", BusinessType.Office,    "Daniel Bekele",   "daniel@apexsolutions.com"),
            ("Green Leaf Restaurant",    "TIN-002-2024", "+251911000002", BusinessType.Restaurant,"Sara Haile",      "sara@greenleaf.com"),
            ("MediCare Health Center",   "TIN-003-2024", "+251911000003", BusinessType.Healthcare,"Yonas Tadesse",   "yonas@medicare.com"),
            ("BrightMind Academy",       "TIN-004-2024", "+251911000004", BusinessType.Education, "Hana Girma",      "hana@brightmind.com"),
            ("TechHub Innovations",      "TIN-005-2024", "+251911000005", BusinessType.Services,  "Mikael Alemu",    "mikael@techhub.com"),
            ("Sunrise Retail Store",     "TIN-006-2024", "+251911000006", BusinessType.Retail,    "Tigist Worku",    "tigist@sunrise.com"),
            ("Capital Law Associates",   "TIN-007-2024", "+251911000007", BusinessType.Services,  "Bereket Negash",  "bereket@capitallaw.com")
        };

        var tenants = tenantData.Select(t => new Tenant
        {
            OrganizationName  = t.Item1,
            TIN               = t.Item2,
            Phone             = t.Item3,
            BusinessType      = t.Item4,
            ContactPersonName = t.Item5,
            ContactEmail      = t.Item6,
            IsActive          = true,
            CreatedAt         = DateTime.UtcNow
        }).ToList();

        context.Tenants.AddRange(tenants);
        await context.SaveChangesAsync();

        // ── 5. Active Leases (one per tenant, link to first 7 units) ────────
        var leaseStartDate = new DateTime(2024, 1, 1, 0, 0, 0, DateTimeKind.Utc);
        var leaseEndDate   = new DateTime(2025, 12, 31, 0, 0, 0, DateTimeKind.Utc);

        var leasedUnits = allUnits.Take(tenants.Count).ToList();
        var leases      = new List<Lease>();

        for (int i = 0; i < tenants.Count; i++)
        {
            var unit   = leasedUnits[i];
            var tenant = tenants[i];

            leases.Add(new Lease
            {
                UnitId        = unit.Id,
                TenantId      = tenant.Id,
                StartDate     = leaseStartDate,
                EndDate       = leaseEndDate,
                MonthlyRent   = unit.MonthlyRent,
                DepositAmount = unit.MonthlyRent * 2,
                Status        = LeaseStatus.Active,
                CreatedAt     = DateTime.UtcNow
            });

            unit.Status    = UnitStatus.Occupied;
            unit.UpdatedAt = DateTime.UtcNow;
        }

        context.Leases.AddRange(leases);
        await context.SaveChangesAsync();

        // ── 6. Invoices (2 months per lease: current + previous month) ───────
        var now      = DateTime.UtcNow;
        var invoices = new List<Invoice>();
        int invoiceCounter = 1;

        foreach (var lease in leases)
        {
            for (int monthOffset = 1; monthOffset >= 0; monthOffset--)
            {
                var period      = now.AddMonths(-monthOffset);
                var dueDate     = new DateTime(period.Year, period.Month,
                                      DateTime.DaysInMonth(period.Year, period.Month),
                                      0, 0, 0, DateTimeKind.Utc);
                var isLastMonth = monthOffset == 1;

                invoices.Add(new Invoice
                {
                    LeaseId       = lease.Id,
                    InvoiceNumber = $"INV-{period:yyyyMM}-{invoiceCounter:D4}",
                    AmountDue     = lease.MonthlyRent,
                    DueDate       = dueDate,
                    IssueDate     = new DateTime(period.Year, period.Month, 1, 0, 0, 0, DateTimeKind.Utc),
                    Status        = isLastMonth ? InvoiceStatus.Paid : InvoiceStatus.Issued,
                    PeriodMonth   = period.Month,
                    PeriodYear    = period.Year,
                    CreatedAt     = DateTime.UtcNow
                });
                invoiceCounter++;
            }
        }

        context.Invoices.AddRange(invoices);
        await context.SaveChangesAsync();

        // ── 7. Payments (pay the previous-month invoices) ────────────────────
        var paidInvoices = invoices.Where(i => i.Status == InvoiceStatus.Paid).ToList();
        var payments     = paidInvoices.Select((inv, idx) => new Payment
        {
            InvoiceId       = inv.Id,
            AmountPaid      = inv.AmountDue,
            PaymentDate     = inv.DueDate.AddDays(-2),
            PaymentMethod   = idx % 2 == 0 ? PaymentMethod.BankTransfer : PaymentMethod.Cash,
            ReferenceNumber = $"TXN-{inv.InvoiceNumber}",
            Notes           = "Seed payment",
            CreatedAt       = DateTime.UtcNow
        }).ToList();

        context.Payments.AddRange(payments);
        await context.SaveChangesAsync();

        // ── 8. Notifications (for the admin user) ────────────────────────────
        var adminUser = await userManager.FindByEmailAsync("admin@bms.com");
        if (adminUser is not null)
        {
            var notifications = new List<Notification>
            {
                new()
                {
                    UserId           = adminUser.Id,
                    Title            = "Welcome to BMS",
                    Message          = "Building Management System has been set up successfully with seed data.",
                    NotificationType = NotificationType.General,
                    IsRead           = false,
                    CreatedAt        = DateTime.UtcNow
                },
                new()
                {
                    UserId           = adminUser.Id,
                    Title            = "Leases Active",
                    Message          = $"{leases.Count} active leases have been created for the seeded tenants.",
                    NotificationType = NotificationType.General,
                    IsRead           = false,
                    CreatedAt        = DateTime.UtcNow
                },
                new()
                {
                    UserId           = adminUser.Id,
                    Title            = "Invoices Issued",
                    Message          = $"{invoices.Count(i => i.Status == InvoiceStatus.Issued)} invoices are currently outstanding and awaiting payment.",
                    NotificationType = NotificationType.PaymentDue,
                    IsRead           = false,
                    CreatedAt        = DateTime.UtcNow
                }
            };

            context.Notifications.AddRange(notifications);
            await context.SaveChangesAsync();
        }

        logger.LogInformation(
            "DataSeeder: completed. Building={Building}, Floors=7, Units={Units}, Tenants={Tenants}, Leases={Leases}, Invoices={Invoices}, Payments={Payments}.",
            building.Name, allUnits.Count, tenants.Count, leases.Count, invoices.Count, payments.Count);
    }
}
