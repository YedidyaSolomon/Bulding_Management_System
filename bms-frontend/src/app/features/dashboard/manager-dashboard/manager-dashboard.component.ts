import { Component, Input, OnInit } from '@angular/core';
import { CommonModule } from '@angular/common';
import { RouterLink } from '@angular/router';
import { MatIconModule } from '@angular/material/icon';
import { MatButtonModule } from '@angular/material/button';
import { HttpClient } from '@angular/common/http';
import { AuthResponse } from '../../../shared/models/auth.models';
import { EtbCurrencyPipe } from '../../../shared/pipes/etb-currency.pipe';
import { environment } from '../../../../environments/environment';

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

interface DashboardSummary {
  totalUnits:          number;
  occupiedUnits:       number;
  availableUnits:      number;
  totalTenants:        number;
  activeLeases:        number;
  totalMonthlyRevenue: number;
  outstandingAmount:   number;
}

interface ApiResponse<T> {
  success: boolean;
  data: T | null;
}

@Component({
  selector: 'app-manager-dashboard',
  standalone: true,
  imports: [CommonModule, RouterLink, MatIconModule, MatButtonModule],
  templateUrl: './manager-dashboard.component.html',
  styleUrls: ['./manager-dashboard.component.scss'],
})
export class ManagerDashboardComponent implements OnInit {
  @Input() user!: AuthResponse;

  readonly today = new Date().toLocaleDateString('en-US', { month: 'long', year: 'numeric' });

  private etb = new EtbCurrencyPipe();

  kpis: KpiCard[] = [
    { label: 'Active Tenants',   value: '—', icon: 'people',       color: 'blue'   },
    { label: 'Active Leases',    value: '—', icon: 'description',  color: 'purple' },
    { label: 'Revenue (Month)',  value: '—', icon: 'payments',     color: 'green'  },
    { label: 'Outstanding',      value: '—', icon: 'receipt_long', color: 'orange' },
    { label: 'Occupied Units',   value: '—', icon: 'apartment',    color: 'teal'   },
    { label: 'Vacant Units',     value: '—', icon: 'door_open',    color: 'red'    },
  ];

  readonly quickActions: QuickAction[] = [
    { label: 'Add Tenant',     icon: 'person_add',    route: '/tenants',       color: 'blue'   },
    { label: 'New Lease',      icon: 'post_add',      route: '/leases',        color: 'purple' },
    { label: 'Record Payment', icon: 'payments',      route: '/payments',      color: 'green'  },
    { label: 'View Reports',   icon: 'bar_chart',     route: '/reports',       color: 'teal'   },
    { label: 'Manage Units',   icon: 'apartment',     route: '/units',         color: 'orange' },
    { label: 'Notifications',  icon: 'notifications', route: '/notifications', color: 'slate'  },
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
            { label: 'Active Tenants',  value: String(d.totalTenants),                    icon: 'people',       color: 'blue'   },
            { label: 'Active Leases',   value: String(d.activeLeases),                    icon: 'description',  color: 'purple' },
            { label: 'Revenue (Month)', value: this.etb.transform(d.totalMonthlyRevenue), icon: 'payments',     color: 'green'  },
            { label: 'Outstanding',     value: this.etb.transform(d.outstandingAmount),   icon: 'receipt_long', color: 'orange' },
            { label: 'Occupied Units',  value: String(d.occupiedUnits),                   icon: 'apartment',    color: 'teal'   },
            { label: 'Vacant Units',    value: String(d.availableUnits),                  icon: 'door_open',    color: 'red'    },
          ];
        },
        error: () => {
          // Keep placeholder values
        },
      });
  }
}
