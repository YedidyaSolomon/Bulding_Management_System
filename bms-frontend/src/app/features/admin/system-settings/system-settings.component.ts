import { Component } from '@angular/core';
import { CommonModule } from '@angular/common';
import { RouterLink } from '@angular/router';
import { FormsModule } from '@angular/forms';
import { MatIconModule } from '@angular/material/icon';
import { MatButtonModule } from '@angular/material/button';
import { MatSlideToggleModule } from '@angular/material/slide-toggle';
import { MatDividerModule } from '@angular/material/divider';

interface SettingGroup {
  title:    string;
  icon:     string;
  settings: SettingItem[];
}

interface SettingItem {
  label:       string;
  description: string;
  type:        'toggle' | 'placeholder';
  value?:      boolean;
}

@Component({
  selector: 'app-system-settings',
  standalone: true,
  imports: [
    CommonModule,
    RouterLink,
    FormsModule,
    MatIconModule,
    MatButtonModule,
    MatSlideToggleModule,
    MatDividerModule,
  ],
  templateUrl: './system-settings.component.html',
  styleUrls: ['./system-settings.component.scss'],
})
export class SystemSettingsComponent {
  readonly settingGroups: SettingGroup[] = [
    {
      title: 'Notifications',
      icon:  'notifications',
      settings: [
        { label: 'Lease expiry alerts',     description: 'Notify managers when leases are expiring within 30 days.',          type: 'toggle', value: true  },
        { label: 'Overdue invoice alerts',  description: 'Send reminders when invoices are past their due date.',              type: 'toggle', value: true  },
        { label: 'Payment confirmations',   description: 'Notify tenants when a payment is successfully recorded.',            type: 'toggle', value: false },
      ],
    },
    {
      title: 'Billing',
      icon:  'payments',
      settings: [
        { label: 'Late fee enforcement',    description: 'Automatically apply late fees after grace period.',                  type: 'toggle', value: false },
        { label: 'Auto-generate invoices',  description: 'Automatically generate monthly rent invoices for active leases.',   type: 'toggle', value: false },
      ],
    },
    {
      title: 'Security',
      icon:  'security',
      settings: [
        { label: 'Require 2FA (Admin)',     description: 'Require two-factor authentication for Admin logins.',                type: 'toggle', value: false },
        { label: 'Session timeout (60 min)',description: 'Force re-login after 60 minutes of inactivity.',                    type: 'toggle', value: true  },
      ],
    },
  ];
}
