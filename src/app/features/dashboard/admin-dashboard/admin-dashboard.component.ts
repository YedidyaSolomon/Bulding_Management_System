import { Component, Input } from '@angular/core';
import { CommonModule } from '@angular/common';
import { RouterLink } from '@angular/router';
import { MatIconModule } from '@angular/material/icon';
import { MatButtonModule } from '@angular/material/button';
import { AuthResponse } from '../../../shared/models/auth.models';

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

@Component({
  selector: 'app-admin-dashboard',
  standalone: true,
  imports: [CommonModule, RouterLink, MatIconModule, MatButtonModule],
  templateUrl: './admin-dashboard.component.html',
  styleUrls: ['./admin-dashboard.component.scss'],
})
export class AdminDashboardComponent {
  @Input() user!: AuthResponse;

  readonly todayLong = new Date().toLocaleDateString('en-US', { weekday: 'long', month: 'long', day: 'numeric', year: 'numeric' });

  readonly kpis: KpiCard[] = [
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
}
