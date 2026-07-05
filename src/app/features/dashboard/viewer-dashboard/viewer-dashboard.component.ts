import { Component, Input } from '@angular/core';
import { CommonModule } from '@angular/common';
import { RouterLink } from '@angular/router';
import { MatIconModule } from '@angular/material/icon';
import { MatButtonModule } from '@angular/material/button';
import { AuthResponse } from '../../../shared/models/auth.models';

@Component({
  selector: 'app-viewer-dashboard',
  standalone: true,
  imports: [CommonModule, RouterLink, MatIconModule, MatButtonModule],
  templateUrl: './viewer-dashboard.component.html',
  styleUrls: ['./viewer-dashboard.component.scss'],
})
export class ViewerDashboardComponent {
  @Input() user!: AuthResponse;

  // Placeholder notification items — will be wired to API in Phase 9
  readonly recentNotifications = [
    { icon: 'info',    color: 'info',    message: 'Your lease renewal is due in 30 days.',         time: '2h ago' },
    { icon: 'receipt', color: 'warning', message: 'Invoice #INV-0042 is outstanding.',              time: '1d ago' },
    { icon: 'check',   color: 'success', message: 'Payment received for Invoice #INV-0039.',        time: '3d ago' },
    { icon: 'event',   color: 'info',    message: 'Scheduled maintenance on 15 July 2026.',         time: '4d ago' },
  ];

  readonly quickLinks = [
    { label: 'View My Lease',    icon: 'description', route: '/leases',       color: 'blue'   },
    { label: 'View Invoices',    icon: 'receipt_long', route: '/invoices',    color: 'purple' },
    { label: 'Unit Details',     icon: 'apartment',   route: '/units',        color: 'teal'   },
    { label: 'Notifications',    icon: 'notifications',route: '/notifications',color: 'orange' },
  ];
}
