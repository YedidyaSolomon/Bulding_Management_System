import { Component, Input, OnInit, OnDestroy, ElementRef, HostListener } from '@angular/core';
import { CommonModule } from '@angular/common';
import { RouterLink } from '@angular/router';
import { MatIconModule } from '@angular/material/icon';
import { MatButtonModule } from '@angular/material/button';
import { MatProgressSpinnerModule } from '@angular/material/progress-spinner';
import { Subject, forkJoin } from 'rxjs';
import { takeUntil, finalize } from 'rxjs/operators';

import { AuthResponse } from '../../../shared/models/auth.models';
import { LeaseDto, LEASE_STATUS_CLASSES } from '../../../shared/models/lease.models';
import {
  InvoiceDto,
  INVOICE_STATUS_CLASSES,
  INVOICE_STATUS_LABELS,
  MONTH_NAMES,
} from '../../../shared/models/invoice.models';
import {
  NotificationDto,
  NOTIFICATION_TYPE_ICONS,
  NOTIFICATION_TYPE_CLASSES,
} from '../../../shared/models/notification.models';
import { LeaseService } from '../../leases/lease.service';
import { InvoiceService } from '../../invoices/invoice.service';
import { NotificationService } from '../../notifications/notification.service';

@Component({
  selector: 'app-viewer-dashboard',
  standalone: true,
  imports: [
    CommonModule,
    RouterLink,
    MatIconModule,
    MatButtonModule,
    MatProgressSpinnerModule,
  ],
  templateUrl: './viewer-dashboard.component.html',
  styleUrls: ['./viewer-dashboard.component.scss'],
})
export class ViewerDashboardComponent implements OnInit, OnDestroy {
  @Input() user!: AuthResponse;

  // ── Quick-access links ────────────────────────────────────────────────────
  readonly quickLinks = [
    { label: 'View My Lease',  icon: 'description',   route: '/leases',        color: 'blue'   },
    { label: 'View Invoices',  icon: 'receipt_long',  route: '/invoices',      color: 'purple' },
    { label: 'Notifications',  icon: 'notifications', route: '/notifications', color: 'teal'   },
    { label: 'Make Payment',   icon: 'payment',       route: '/payments',      color: 'orange' },
  ];

  // ── Data ─────────────────────────────────────────────────────────────────
  leases:        LeaseDto[]        = [];
  invoices:      InvoiceDto[]      = [];
  notifications: NotificationDto[] = [];
  loading = true;

  private destroy$ = new Subject<void>();

  // ── Cursor-glow element ref ───────────────────────────────────────────────
  private glowEl: HTMLElement | null = null;

  constructor(
    private leaseService:        LeaseService,
    private invoiceService:      InvoiceService,
    private notificationService: NotificationService,
    private elRef:               ElementRef<HTMLElement>,
  ) {}

  /** Move the radial glow to follow the cursor.
   *  The glow layer is position:fixed, so we use viewport coordinates directly.
   */
  @HostListener('mousemove', ['$event'])
  onMouseMove(e: MouseEvent): void {
    if (!this.glowEl) {
      this.glowEl = this.elRef.nativeElement.querySelector('.cursor-glow');
    }
    if (!this.glowEl) return;
    // clientX/Y are viewport-relative — matches position:fixed coordinate space
    this.glowEl.style.setProperty('--gx', `${e.clientX}px`);
    this.glowEl.style.setProperty('--gy', `${e.clientY}px`);
  }

  // ── Lifecycle ─────────────────────────────────────────────────────────────
  ngOnInit(): void {
    forkJoin({
      leases:        this.leaseService.getAll(),          // scoped to user by backend role check
      invoices:      this.invoiceService.getMyInvoices(), // GET /api/invoices/mine
      notifications: this.notificationService.getAll(),   // GET /api/notifications
    })
      .pipe(takeUntil(this.destroy$), finalize(() => (this.loading = false)))
      .subscribe({
        next: ({ leases, invoices, notifications }) => {
          this.leases   = leases;
          this.invoices = invoices;
          // Show only the 5 most-recent notifications in the dashboard widget
          this.notifications = [...notifications]
            .sort((a, b) =>
              new Date(b.createdAt).getTime() - new Date(a.createdAt).getTime()
            )
            .slice(0, 5);
        },
        error: () => {},
      });
  }

  ngOnDestroy(): void {
    this.destroy$.next();
    this.destroy$.complete();
  }

  // ── Lease helpers ─────────────────────────────────────────────────────────
  get activeLease(): LeaseDto | undefined {
    return this.leases.find(l => l.status === 'Active' || l.status === 'PendingRenewal');
  }

  get hasActiveLease(): boolean { return !!this.activeLease; }

  get daysUntilLeaseExpiry(): number | null {
    const l = this.activeLease;
    if (!l) return null;
    return Math.ceil(
      (new Date(l.endDate).getTime() - Date.now()) / 86_400_000
    );
  }

  get leaseExpiryUrgency(): 'ok' | 'warn' | 'danger' {
    const d = this.daysUntilLeaseExpiry;
    if (d === null) return 'ok';
    if (d <= 7)     return 'danger';
    if (d <= 30)    return 'warn';
    return 'ok';
  }

  leaseStatusClass(status: string): string {
    return LEASE_STATUS_CLASSES[status] ?? 'badge-neutral';
  }

  // ── Invoice helpers ───────────────────────────────────────────────────────
  get totalOutstanding(): number {
    return this.invoices
      .filter(i => i.status === 'Issued' || i.status === 'Overdue')
      .reduce((sum, i) => sum + i.amountDue, 0);
  }

  get overdueInvoices(): InvoiceDto[] {
    return this.invoices.filter(i => i.status === 'Overdue');
  }

  get recentInvoices(): InvoiceDto[] {
    return [...this.invoices]
      .sort((a, b) => new Date(b.issueDate).getTime() - new Date(a.issueDate).getTime())
      .slice(0, 4);
  }

  invoiceStatusClass(status: string): string {
    return INVOICE_STATUS_CLASSES[status] ?? 'badge-neutral';
  }

  invoiceStatusLabel(status: string): string {
    return INVOICE_STATUS_LABELS[status] ?? status;
  }

  periodLabel(inv: InvoiceDto): string {
    const month = MONTH_NAMES[inv.periodMonth - 1] ?? '';
    return `${month} ${inv.periodYear}`;
  }

  // ── Notification helpers ──────────────────────────────────────────────────
  get unreadCount(): number {
    return this.notifications.filter(n => !n.isRead).length;
  }

  notifIcon(type: string):  string { return NOTIFICATION_TYPE_ICONS[type]   ?? 'info'; }
  notifClass(type: string): string { return NOTIFICATION_TYPE_CLASSES[type] ?? 'type-general'; }

  timeAgo(isoDate: string): string {
    const diff  = Date.now() - new Date(isoDate).getTime();
    const mins  = Math.floor(diff / 60_000);
    const hours = Math.floor(diff / 3_600_000);
    const days  = Math.floor(diff / 86_400_000);
    if (mins  < 1)  return 'just now';
    if (mins  < 60) return `${mins}m ago`;
    if (hours < 24) return `${hours}h ago`;
    if (days  < 7)  return `${days}d ago`;
    return new Date(isoDate).toLocaleDateString('en-US', { month: 'short', day: 'numeric' });
  }
}
