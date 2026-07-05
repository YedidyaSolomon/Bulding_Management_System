import { Component, Input, OnInit } from '@angular/core';
import { CommonModule } from '@angular/common';
import { RouterLink } from '@angular/router';
import { MatIconModule } from '@angular/material/icon';
import { MatButtonModule } from '@angular/material/button';
import { MatProgressSpinnerModule } from '@angular/material/progress-spinner';
import { HttpClient } from '@angular/common/http';
import { AuthResponse } from '../../../shared/models/auth.models';
import { EtbCurrencyPipe } from '../../../shared/pipes/etb-currency.pipe';
import { environment } from '../../../../environments/environment';

interface KpiCard {
  label: string;
  value: string;
  icon:  string;
  bg:    string;
  route: string;
}

interface ShortcutCard {
  label:       string;
  description: string;
  icon:        string;
  route:       string;
  color:       string;
}

interface DashboardSummary {
  totalUnits:             number;
  occupiedUnits:          number;
  availableUnits:         number;
  totalTenants:           number;
  activeLeases:           number;
  expiringLeasesIn30Days: number;
  totalMonthlyRevenue:    number;
  outstandingAmount:      number;
  overdueInvoices:        number;
  unreadNotifications:    number;
}

interface ApiResponse<T> {
  success: boolean;
  data: T | null;
}

@Component({
  selector: 'app-admin-dashboard',
  standalone: true,
  imports: [CommonModule, RouterLink, MatIconModule, MatButtonModule, MatProgressSpinnerModule],
  templateUrl: './admin-dashboard.component.html',
  styleUrls: ['./admin-dashboard.component.scss'],
})
export class AdminDashboardComponent implements OnInit {
  @Input() user!: AuthResponse;

  readonly todayLong = new Date().toLocaleDateString('en-US', { weekday: 'long', month: 'long', day: 'numeric', year: 'numeric' });

  private etb = new EtbCurrencyPipe();

  kpis: KpiCard[] = [
    { label: 'Total Units',      value: '—', icon: 'apartment',          bg: '#2563EB', route: '/units'         },
    { label: 'Occupied',         value: '—', icon: 'meeting_room',       bg: '#10B981', route: '/units'         },
    { label: 'Vacant',           value: '—', icon: 'door_open',          bg: '#F59E0B', route: '/units'         },
    { label: 'Active Tenants',   value: '—', icon: 'people',             bg: '#7C3AED', route: '/tenants'       },
    { label: 'Active Leases',    value: '—', icon: 'description',        bg: '#0D9488', route: '/leases'        },
    { label: 'Monthly Revenue',  value: '—', icon: 'trending_up',        bg: '#059669', route: '/reports'       },
    { label: 'Outstanding',      value: '—', icon: 'warning',            bg: '#EF4444', route: '/invoices'      },
    { label: 'Registered Users', value: '—', icon: 'manage_accounts',    bg: '#6366F1', route: '/admin/users'   },
  ];

  readonly shortcuts: ShortcutCard[] = [
    {
      label:       'User Management',
      description: 'Manage users, assign roles, promote to Manager',
      icon:        'manage_accounts',
      route:       '/admin/users',
      color:       'indigo',
    },
    {
      label:       'Register Manager',
      description: 'Create a new Manager account directly',
      icon:        'person_add',
      route:       '/admin/register-manager',
      color:       'blue',
    },
    {
      label:       'System Settings',
      description: 'Configure application-wide settings',
      icon:        'settings',
      route:       '/admin/settings',
      color:       'slate',
    },
    {
      label:       'All Reports',
      description: 'Occupancy, revenue, arrears, and lease expiry',
      icon:        'assessment',
      route:       '/reports',
      color:       'teal',
    },
    {
      label:       'Notifications',
      description: 'System notifications and alerts',
      icon:        'notifications',
      route:       '/notifications',
      color:       'orange',
    },
    {
      label:       'Payments',
      description: 'Review and process all payments',
      icon:        'payments',
      route:       '/payments',
      color:       'green',
    },
  ];

  constructor(private http: HttpClient) {}

  ngOnInit(): void {
    this.http
      .get<ApiResponse<DashboardSummary>>(`${environment.apiUrl}/dashboard`)
      .subscribe({
        next: res => {
          if (!res?.data) return;
          const d = res.data;
          this.kpis = [
            { label: 'Total Units',     value: String(d.totalUnits),                        icon: 'apartment',       bg: '#2563EB', route: '/units'       },
            { label: 'Occupied',        value: String(d.occupiedUnits),                     icon: 'meeting_room',    bg: '#10B981', route: '/units'       },
            { label: 'Vacant',          value: String(d.availableUnits),                    icon: 'door_open',       bg: '#F59E0B', route: '/units'       },
            { label: 'Active Tenants',  value: String(d.totalTenants),                      icon: 'people',          bg: '#7C3AED', route: '/tenants'     },
            { label: 'Active Leases',   value: String(d.activeLeases),                      icon: 'description',     bg: '#0D9488', route: '/leases'      },
            { label: 'Monthly Revenue', value: this.etb.transform(d.totalMonthlyRevenue),   icon: 'trending_up',     bg: '#059669', route: '/reports'     },
            { label: 'Outstanding',     value: this.etb.transform(d.outstandingAmount),     icon: 'warning',         bg: '#EF4444', route: '/invoices'    },
            { label: 'Overdue Inv.',    value: String(d.overdueInvoices),                   icon: 'manage_accounts', bg: '#6366F1', route: '/admin/users' },
          ];
        },
        error: () => {
          // Keep placeholder values — API not connected yet
        },
      });
  }
}
