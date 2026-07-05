import { Component } from '@angular/core';
import { CommonModule } from '@angular/common';
import { MatIconModule } from '@angular/material/icon';

@Component({
  selector: 'app-leases',
  standalone: true,
  imports: [CommonModule, MatIconModule],
  template: `
    <div class="bms-page fade-in">
      <div class="bms-page-header"><div><h1>Leases</h1><p>Manage lease agreements</p></div></div>
      <div class="empty-state"><mat-icon>description</mat-icon><h3>Leases — Phase 5</h3><p>Lease management will be built in Phase 5.</p></div>
    </div>`
})
export class LeasesComponent {}
