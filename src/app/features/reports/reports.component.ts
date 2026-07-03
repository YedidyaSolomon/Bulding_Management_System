import { Component } from '@angular/core';
import { CommonModule } from '@angular/common';
import { MatIconModule } from '@angular/material/icon';

@Component({
  selector: 'app-reports',
  standalone: true,
  imports: [CommonModule, MatIconModule],
  template: `
    <div class="bms-page fade-in">
      <div class="bms-page-header"><div><h1>Reports</h1><p>Analytics &amp; reporting</p></div></div>
      <div class="empty-state"><mat-icon>bar_chart</mat-icon><h3>Reports — Phase 8</h3><p>Occupancy, revenue, and arrears reports will be built in Phase 8.</p></div>
    </div>`
})
export class ReportsComponent {}
