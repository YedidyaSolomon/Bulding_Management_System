import { Component } from '@angular/core';
import { CommonModule } from '@angular/common';
import { MatIconModule } from '@angular/material/icon';

@Component({
  selector: 'app-tenants',
  standalone: true,
  imports: [CommonModule, MatIconModule],
  template: `
    <div class="bms-page fade-in">
      <div class="bms-page-header"><div><h1>Tenants</h1><p>Manage tenants</p></div></div>
      <div class="empty-state"><mat-icon>people</mat-icon><h3>Tenants — Phase 4</h3><p>Tenant management will be built in Phase 4.</p></div>
    </div>`
})
export class TenantsComponent {}
