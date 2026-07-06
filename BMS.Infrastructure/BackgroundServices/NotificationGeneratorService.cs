using BMS.Application.Interfaces.Repositories;
using BMS.Infrastructure.Data;
using BMS.Infrastructure.Entities;
using BMS.Infrastructure.Enums;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace BMS.Infrastructure.BackgroundServices;

/// <summary>
/// Background service that generates notifications daily for:
/// - Invoices due within 7 days (PaymentDue)
/// - Overdue invoices (PaymentOverdue)
/// - Leases expiring within 30 days (LeaseExpiry)
/// - Documents expiring within 30 days (DocumentExpiry)
/// </summary>
public class NotificationGeneratorService : BackgroundService
{
    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger<NotificationGeneratorService> _logger;
    private readonly TimeSpan _runInterval = TimeSpan.FromHours(24);

    // Configuration constants
    private const int PAYMENT_DUE_DAYS      = 7;
    private const int LEASE_EXPIRY_DAYS     = 30;
    private const int DOCUMENT_EXPIRY_DAYS  = 30;

    public NotificationGeneratorService(
        IServiceProvider serviceProvider,
        ILogger<NotificationGeneratorService> logger)
    {
        _serviceProvider = serviceProvider;
        _logger          = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("NotificationGeneratorService started. Will run every {Interval}.", _runInterval);

        // Run immediately on startup, then every 24 hours
        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await GenerateNotificationsAsync(stoppingToken);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred while generating notifications.");
            }

            // Wait 24 hours before next run (or until cancellation)
            await Task.Delay(_runInterval, stoppingToken);
        }

        _logger.LogInformation("NotificationGeneratorService stopped.");
    }

    private async Task GenerateNotificationsAsync(CancellationToken cancellationToken)
    {
        _logger.LogInformation("Starting notification generation at {Time}.", DateTime.UtcNow);

        using var scope = _serviceProvider.CreateScope();
        var context            = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        var invoiceRepository  = scope.ServiceProvider.GetRequiredService<IInvoiceRepository>();
        var leaseRepository    = scope.ServiceProvider.GetRequiredService<ILeaseRepository>();
        var documentRepository = scope.ServiceProvider.GetRequiredService<IDocumentRepository>();

        var created = 0;

        // ── 1. Payment Due (invoices due within 7 days) ──────────────────────
        created += await GeneratePaymentDueNotificationsAsync(context, invoiceRepository, cancellationToken);

        // ── 2. Payment Overdue (overdue invoices) ────────────────────────────
        created += await GeneratePaymentOverdueNotificationsAsync(context, invoiceRepository, cancellationToken);

        // ── 3. Lease Expiry (leases expiring within 30 days) ─────────────────
        created += await GenerateLeaseExpiryNotificationsAsync(context, leaseRepository, cancellationToken);

        // ── 4. Document Expiry (documents expiring within 30 days) ───────────
        created += await GenerateDocumentExpiryNotificationsAsync(context, documentRepository, cancellationToken);

        _logger.LogInformation(
            "Notification generation completed. Created {Count} new notifications.",
            created);
    }

    // ══════════════════════════════════════════════════════════════════════════
    // Payment Due Notifications
    // ══════════════════════════════════════════════════════════════════════════

    private async Task<int> GeneratePaymentDueNotificationsAsync(
        ApplicationDbContext context,
        IInvoiceRepository invoiceRepository,
        CancellationToken cancellationToken)
    {
        var now     = DateTime.UtcNow.Date;
        var cutoff  = now.AddDays(PAYMENT_DUE_DAYS);

        // Get invoices that are Issued and due within PAYMENT_DUE_DAYS
        var dueInvoices = await context.Invoices
            .Include(i => i.Lease)
                .ThenInclude(l => l.Tenant)
            .Where(i =>
                i.Status == InvoiceStatus.Issued &&
                i.DueDate.Date >= now &&
                i.DueDate.Date <= cutoff)
            .ToListAsync(cancellationToken);

        var created = 0;

        foreach (var invoice in dueInvoices)
        {
            var userId = invoice.Lease.Tenant.UserId;

            // Check if a PaymentDue notification for this invoice already exists
            var exists = await context.Notifications
                .AnyAsync(n =>
                    n.UserId == userId &&
                    n.NotificationType == NotificationType.PaymentDue &&
                    n.Message.Contains(invoice.InvoiceNumber),
                    cancellationToken);

            if (exists) continue;

            var daysUntilDue = (invoice.DueDate.Date - now).Days;
            var notification = new Notification
            {
                UserId           = userId,
                Title            = "Payment Due Soon",
                Message          = $"Invoice {invoice.InvoiceNumber} for ${invoice.AmountDue:N2} is due in {daysUntilDue} day(s) on {invoice.DueDate:MMM dd, yyyy}.",
                NotificationType = NotificationType.PaymentDue,
                IsRead           = false,
                CreatedAt        = DateTime.UtcNow,
            };

            context.Notifications.Add(notification);
            created++;
        }

        if (created > 0)
            await context.SaveChangesAsync(cancellationToken);

        return created;
    }

    // ══════════════════════════════════════════════════════════════════════════
    // Payment Overdue Notifications
    // ══════════════════════════════════════════════════════════════════════════

    private async Task<int> GeneratePaymentOverdueNotificationsAsync(
        ApplicationDbContext context,
        IInvoiceRepository invoiceRepository,
        CancellationToken cancellationToken)
    {
        // GetOverdueAsync() auto-marks Issued invoices past DueDate as Overdue
        var overdueInvoices = await invoiceRepository.GetOverdueAsync();
        var created = 0;

        foreach (var invoiceDto in overdueInvoices)
        {
            // Fetch the full invoice with navigation properties
            var invoice = await context.Invoices
                .Include(i => i.Lease)
                    .ThenInclude(l => l.Tenant)
                .FirstOrDefaultAsync(i => i.Id == invoiceDto.Id, cancellationToken);

            if (invoice is null) continue;

            var userId = invoice.Lease.Tenant.UserId;

            // Check if a PaymentOverdue notification for this invoice already exists
            var exists = await context.Notifications
                .AnyAsync(n =>
                    n.UserId == userId &&
                    n.NotificationType == NotificationType.PaymentOverdue &&
                    n.Message.Contains(invoice.InvoiceNumber),
                    cancellationToken);

            if (exists) continue;

            var daysOverdue = (DateTime.UtcNow.Date - invoice.DueDate.Date).Days;
            var notification = new Notification
            {
                UserId           = userId,
                Title            = "Payment Overdue",
                Message          = $"Invoice {invoice.InvoiceNumber} for ${invoice.AmountDue:N2} is overdue by {daysOverdue} day(s). Please pay immediately to avoid penalties.",
                NotificationType = NotificationType.PaymentOverdue,
                IsRead           = false,
                CreatedAt        = DateTime.UtcNow,
            };

            context.Notifications.Add(notification);
            created++;
        }

        if (created > 0)
            await context.SaveChangesAsync(cancellationToken);

        return created;
    }

    // ══════════════════════════════════════════════════════════════════════════
    // Lease Expiry Notifications
    // ══════════════════════════════════════════════════════════════════════════

    private async Task<int> GenerateLeaseExpiryNotificationsAsync(
        ApplicationDbContext context,
        ILeaseRepository leaseRepository,
        CancellationToken cancellationToken)
    {
        var expiringLeases = await leaseRepository.GetExpiringAsync(LEASE_EXPIRY_DAYS);
        var created = 0;

        foreach (var lease in expiringLeases)
        {
            // Check if a LeaseExpiry notification for this lease already exists
            var exists = await context.Notifications
                .AnyAsync(n =>
                    n.UserId == lease.UserId &&
                    n.NotificationType == NotificationType.LeaseExpiry &&
                    n.Message.Contains($"Unit {lease.UnitNumber}"),
                    cancellationToken);

            if (exists) continue;

            var daysUntilExpiry = (lease.EndDate.Date - DateTime.UtcNow.Date).Days;
            var notification = new Notification
            {
                UserId           = lease.UserId,
                Title            = "Lease Expiring Soon",
                Message          = $"Your lease for Unit {lease.UnitNumber} expires in {daysUntilExpiry} day(s) on {lease.EndDate:MMM dd, yyyy}. Please contact management to renew.",
                NotificationType = NotificationType.LeaseExpiry,
                IsRead           = false,
                CreatedAt        = DateTime.UtcNow,
            };

            context.Notifications.Add(notification);
            created++;
        }

        if (created > 0)
            await context.SaveChangesAsync(cancellationToken);

        return created;
    }

    // ══════════════════════════════════════════════════════════════════════════
    // Document Expiry Notifications
    // ══════════════════════════════════════════════════════════════════════════

    private async Task<int> GenerateDocumentExpiryNotificationsAsync(
        ApplicationDbContext context,
        IDocumentRepository documentRepository,
        CancellationToken cancellationToken)
    {
        var expiringDocuments = await documentRepository.GetExpiringAsync(DOCUMENT_EXPIRY_DAYS);
        var created = 0;

        foreach (var doc in expiringDocuments)
        {
            // Check if a DocumentExpiry notification for this document already exists
            var exists = await context.Notifications
                .AnyAsync(n =>
                    n.UserId == doc.UserId &&
                    n.NotificationType == NotificationType.DocumentExpiry &&
                    n.Message.Contains(doc.DocumentType),
                    cancellationToken);

            if (exists) continue;

            var daysUntilExpiry = (doc.ExpiryDate.Date - DateTime.UtcNow.Date).Days;
            var notification = new Notification
            {
                UserId           = doc.UserId,
                Title            = "Document Expiring Soon",
                Message          = $"Your {doc.DocumentType} document expires in {daysUntilExpiry} day(s) on {doc.ExpiryDate:MMM dd, yyyy}. Please upload a renewed copy.",
                NotificationType = NotificationType.DocumentExpiry,
                IsRead           = false,
                CreatedAt        = DateTime.UtcNow,
            };

            context.Notifications.Add(notification);
            created++;
        }

        if (created > 0)
            await context.SaveChangesAsync(cancellationToken);

        return created;
    }
}
