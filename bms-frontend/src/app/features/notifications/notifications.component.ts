import { Component, OnInit, OnDestroy } from '@angular/core';
import { CommonModule } from '@angular/common';
import { ReactiveFormsModule, FormControl } from '@angular/forms';
import { Router } from '@angular/router';
import { trigger, transition, style, animate, query, stagger } from '@angular/animations';
import { MatButtonModule } from '@angular/material/button';
import { MatIconModule } from '@angular/material/icon';
import { MatSelectModule } from '@angular/material/select';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatProgressSpinnerModule } from '@angular/material/progress-spinner';
import { MatSnackBar, MatSnackBarModule } from '@angular/material/snack-bar';
import { MatTooltipModule } from '@angular/material/tooltip';
import { MatDividerModule } from '@angular/material/divider';
import { Subject } from 'rxjs';
import { takeUntil, finalize } from 'rxjs/operators';

import { NotificationService } from './notification.service';
import {
  NotificationDto,
  NOTIFICATION_TYPES,
  NOTIFICATION_TYPE_LABELS,
  NOTIFICATION_TYPE_ICONS,
  NOTIFICATION_TYPE_CLASSES,
} from '../../shared/models/notification.models';

@Component({
  selector: 'app-notifications',
  standalone: true,
  imports: [
    CommonModule,
    ReactiveFormsModule,
    MatButtonModule,
    MatIconModule,
    MatSelectModule,
    MatFormFieldModule,
    MatProgressSpinnerModule,
    MatSnackBarModule,
    MatTooltipModule,
    MatDividerModule,
  ],
  templateUrl: './notifications.component.html',
  styleUrls: ['./notifications.component.scss'],
  animations: [
    trigger('listAnimation', [
      transition('* => *', [
        query(':enter', [
          style({ opacity: 0, transform: 'translateY(10px)' }),
          stagger(40, [
            animate('280ms cubic-bezier(0.35, 0, 0.25, 1)', style({ opacity: 1, transform: 'translateY(0)' }))
          ])
        ], { optional: true })
      ])
    ])
  ]
})
export class NotificationsComponent implements OnInit, OnDestroy {

  allNotifications:       NotificationDto[] = [];
  filteredNotifications:  NotificationDto[] = [];
  loading        = false;
  markingAll     = false;

  typeFilter = new FormControl('all');

  readonly notificationTypes      = NOTIFICATION_TYPES;
  readonly notificationTypeLabels = NOTIFICATION_TYPE_LABELS;
  readonly notificationTypeIcons  = NOTIFICATION_TYPE_ICONS;
  readonly notificationTypeClasses = NOTIFICATION_TYPE_CLASSES;

  private destroy$ = new Subject<void>();

  constructor(
    private notificationService: NotificationService,
    private snackBar: MatSnackBar,
    private router: Router,
  ) {}

  // ── Stats ──────────────────────────────────────────────────────────────────

  get totalCount():  number { return this.allNotifications.length; }
  get unreadCount(): number { return this.allNotifications.filter(n => !n.isRead).length; }
  get readCount():   number { return this.allNotifications.filter(n => n.isRead).length; }

  get hasUnread(): boolean { return this.unreadCount > 0; }

  ngOnInit(): void {
    this.loadNotifications();

    this.typeFilter.valueChanges
      .pipe(takeUntil(this.destroy$))
      .subscribe(() => this.applyFilter());
  }

  ngOnDestroy(): void {
    this.destroy$.next();
    this.destroy$.complete();
  }

  loadNotifications(): void {
    this.loading = true;
    this.notificationService.getAll()
      .pipe(takeUntil(this.destroy$), finalize(() => (this.loading = false)))
      .subscribe({
        next: items => {
          // Sort: unread first, then newest first
          this.allNotifications = [...items].sort((a, b) => {
            if (a.isRead !== b.isRead) return a.isRead ? 1 : -1;
            return new Date(b.createdAt).getTime() - new Date(a.createdAt).getTime();
          });
          this.applyFilter();
        },
        error: () => {},
      });
  }

  applyFilter(): void {
    const type = this.typeFilter.value ?? 'all';
    this.filteredNotifications = type === 'all'
      ? this.allNotifications
      : this.allNotifications.filter(n => n.notificationType === type);
  }

  onMarkAsRead(notification: NotificationDto): void {
    if (notification.isRead) return;
    this.notificationService.markAsRead(notification.id).subscribe({
      next: updated => {
        const idx = this.allNotifications.findIndex(n => n.id === notification.id);
        if (idx !== -1) {
          this.allNotifications[idx] = updated;
          this.allNotifications = [...this.allNotifications];
          this.applyFilter();
          this.notificationService.setUnreadCount(this.unreadCount);
        }
      },
      error: () => {
        this.snackBar.open('Failed to mark as read.', 'Dismiss', { duration: 4000, panelClass: ['snack-error'] });
      },
    });
  }

  /** Mark as read then navigate to the relevant page. */
  onCardClick(notification: NotificationDto): void {
    const navigate = () => {
      const route = this.deepLinkRoute(notification.notificationType);
      if (route) this.router.navigate(route.commands, { queryParams: route.queryParams });
    };

    if (!notification.isRead) {
      // Mark read first, then navigate regardless of outcome
      this.notificationService.markAsRead(notification.id).subscribe({
        next: updated => {
          const idx = this.allNotifications.findIndex(n => n.id === notification.id);
          if (idx !== -1) {
            this.allNotifications[idx] = updated;
            this.allNotifications = [...this.allNotifications];
            this.applyFilter();
            this.notificationService.setUnreadCount(this.unreadCount);
          }
          navigate();
        },
        error: () => navigate(), // Navigate even if marking-read fails
      });
    } else {
      navigate();
    }
  }

  /**
   * Maps a NotificationType to a router link + optional query params.
   * Returns null for types with no specific deep-link target.
   */
  private deepLinkRoute(type: string): { commands: any[]; queryParams?: Record<string, string> } | null {
    switch (type) {
      case 'PaymentDue':
        return { commands: ['/invoices'], queryParams: { status: 'Issued' } };
      case 'PaymentOverdue':
        return { commands: ['/invoices'], queryParams: { status: 'Overdue' } };
      case 'LeaseExpiry':
        return { commands: ['/leases'], queryParams: { status: 'Active' } };
      case 'DocumentExpiry':
        return { commands: ['/tenants'] };
      default:
        return null; // General — no specific target
    }
  }

  /** Tooltip text shown on each card to indicate where it navigates. */
  navTooltip(type: string): string {
    switch (type) {
      case 'PaymentDue':      return 'View outstanding invoices';
      case 'PaymentOverdue':  return 'View overdue invoices';
      case 'LeaseExpiry':     return 'View active leases';
      case 'DocumentExpiry':  return 'View tenant documents';
      default:                return '';
    }
  }

  onMarkAllAsRead(): void {
    if (!this.hasUnread) return;
    this.markingAll = true;
    this.notificationService.markAllAsRead()
      .pipe(finalize(() => (this.markingAll = false)))
      .subscribe({
        next: () => {
          this.allNotifications = this.allNotifications.map(n => ({ ...n, isRead: true }));
          this.applyFilter();
          this.notificationService.setUnreadCount(0);
          this.snackBar.open('All notifications marked as read.', undefined, {
            duration: 3000, panelClass: ['snack-success'],
          });
        },
        error: () => {
          this.snackBar.open('Failed to mark all as read.', 'Dismiss', { duration: 4000, panelClass: ['snack-error'] });
        },
      });
  }

  clearFilter(): void {
    this.typeFilter.setValue('all');
  }

  get hasActiveFilter(): boolean {
    return this.typeFilter.value !== 'all';
  }

  typeLabel(type: string):   string { return NOTIFICATION_TYPE_LABELS[type]   ?? type; }
  typeIcon(type: string):    string { return NOTIFICATION_TYPE_ICONS[type]    ?? 'info'; }
  typeClass(type: string):   string { return NOTIFICATION_TYPE_CLASSES[type]  ?? 'type-general'; }

  /** Human-readable relative time, e.g. "2 hours ago" */
  timeAgo(isoDate: string): string {
    const diff = Date.now() - new Date(isoDate).getTime();
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
