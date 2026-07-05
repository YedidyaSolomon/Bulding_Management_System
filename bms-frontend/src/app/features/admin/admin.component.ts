import { Component } from '@angular/core';
import { CommonModule } from '@angular/common';
import { RouterLink } from '@angular/router';
import { MatIconModule } from '@angular/material/icon';
import { MatButtonModule } from '@angular/material/button';

interface AdminSection {
  title:       string;
  description: string;
  icon:        string;
  route:       string;
  color:       string;
  badge?:      string;
}

@Component({
  selector: 'app-admin',
  standalone: true,
  imports: [CommonModule, RouterLink, MatIconModule, MatButtonModule],
  templateUrl: './admin.component.html',
  styleUrls: ['./admin.component.scss'],
})
export class AdminComponent {
  readonly sections: AdminSection[] = [
    {
      title:       'User Management',
      description: 'View all registered users, assign or change roles (Viewer ↔ Manager). Admin role is seeded and cannot be assigned here.',
      icon:        'manage_accounts',
      route:       '/admin/users',
      color:       'indigo',
    },
    {
      title:       'Register Manager',
      description: 'Create a new Manager account directly without going through the public registration flow.',
      icon:        'person_add',
      route:       '/admin/register-manager',
      color:       'blue',
    },
    {
      title:       'System Settings',
      description: 'Configure application-wide parameters such as late fees, notification thresholds, and system defaults.',
      icon:        'settings',
      route:       '/admin/settings',
      color:       'slate',
      badge:       'Placeholder',
    },
  ];
}
