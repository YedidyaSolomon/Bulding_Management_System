import { Component, Input } from '@angular/core';
import { CommonModule } from '@angular/common';
import { RouterLink } from '@angular/router';
import { MatIconModule } from '@angular/material/icon';
import { MatButtonModule } from '@angular/material/button';
import { AuthResponse } from '../../../shared/models/auth.models';

interface KpiCard {
  label:    string;
  value:    string;
  icon:     string;
  color:    string;
  trend?:   string;
  trendUp?: boolean;
}

interface QuickAction {
  label: string;
  icon:  string;
  route: string;
  color: string;
}

@Component({
  selector: 'app-manager-dashboard',
  standalone: true,
  imports: [CommonModule, RouterLink, MatIconModule, MatButtonModule],
  templateUrl: './manager-dashboard.component.html',
  styleUrls: ['./manager-dashboard.component.scss'],
})
export class ManagerDashboardComponent {
  @Input() user!: AuthResponse;

  readonly today = new Date().toLocaleDateString('en-US', { month: 'long', year: 'numeric' });

  // Placeholder KPIs — wired to API in Phase 2
  readonly kpis: KpiCard[] = [
    { label: 'Active Tenants',   value: '—', icon: 'people',       color: 'blue',   trend: 'Loading…' },
    { label: 'Active Leases',    value: '—', icon: 'description',  color: 'purple', trend: 'Loading…' },
    { label: 'Payments (Month)', value: '—', icon: 'payments',     color: 'green',  trend: 'Loading…' },
    { label: 'Outstanding',      value: '—', icon: 'receipt_long', color: 'orange', trend: 'Loading…' },
    { label: 'Occupied Units',   value: '—', icon: 'apartment',    color: 'teal',   trend: 'Loading…' },
    { label: 'Vacant Units',     value: '—', icon: 'door_open',    color: 'red',    trend: 'Loading…' },
  ];

  readonly quickActions: QuickAction[] = [
    { label: 'Add Tenant',     icon: 'person_add',   route: '/tenants',   color: 'blue'   },
    { label: 'New Lease',      icon: 'post_add',     route: '/leases',    color: 'purple' },
    { label: 'Record Payment', icon: 'payments',     route: '/payments',  color: 'green'  },
    { label: 'View Reports',   icon: 'bar_chart',    route: '/reports',   color: 'teal'   },
    { label: 'Manage Units',   icon: 'apartment',    route: '/units',     color: 'orange' },
    { label: 'Notifications',  icon: 'notifications',route: '/notifications', color: 'slate' },
  ];
}
