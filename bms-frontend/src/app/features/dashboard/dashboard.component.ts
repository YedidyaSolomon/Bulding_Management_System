import { Component, OnInit } from '@angular/core';
import { CommonModule } from '@angular/common';
import { AuthService } from '../../core/services/auth.service';
import { AuthResponse, UserRole } from '../../shared/models/auth.models';
import { ViewerDashboardComponent }  from './viewer-dashboard/viewer-dashboard.component';
import { ManagerDashboardComponent } from './manager-dashboard/manager-dashboard.component';
import { AdminDashboardComponent }   from './admin-dashboard/admin-dashboard.component';

@Component({
  selector: 'app-dashboard',
  standalone: true,
  imports: [
    CommonModule,
    ViewerDashboardComponent,
    ManagerDashboardComponent,
    AdminDashboardComponent,
  ],
  template: `
    <div class="bms-page fade-in" *ngIf="user">
      <app-admin-dashboard   *ngIf="role === 'Admin'"   [user]="user" />
      <app-manager-dashboard *ngIf="role === 'Manager'" [user]="user" />
      <app-viewer-dashboard  *ngIf="role === 'Viewer'"  [user]="user" />
    </div>
  `,
})
export class DashboardComponent implements OnInit {
  user: AuthResponse | null = null;
  role: UserRole | null     = null;

  constructor(private authService: AuthService) {}

  ngOnInit(): void {
    this.user = this.authService.getCurrentUser();
    this.role = this.user?.role as UserRole ?? null;
  }
}
