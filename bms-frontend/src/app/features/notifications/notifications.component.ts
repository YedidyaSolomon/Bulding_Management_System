import { Component } from '@angular/core';
import { CommonModule } from '@angular/common';
import { MatIconModule } from '@angular/material/icon';

@Component({
  selector: 'app-notifications',
  standalone: true,
  imports: [CommonModule, MatIconModule],
  template: `
    <div class="bms-page fade-in">
      <div class="bms-page-header"><div><h1>Notifications</h1><p>View system notifications</p></div></div>
      <div class="empty-state"><mat-icon>notifications</mat-icon><h3>Notifications — Phase 9</h3><p>Notification centre will be built in Phase 9.</p></div>
    </div>`
})
export class NotificationsComponent {}
