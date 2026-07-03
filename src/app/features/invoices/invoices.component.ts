import { Component } from '@angular/core';
import { CommonModule } from '@angular/common';
import { MatIconModule } from '@angular/material/icon';

@Component({
  selector: 'app-invoices',
  standalone: true,
  imports: [CommonModule, MatIconModule],
  template: `
    <div class="bms-page fade-in">
      <div class="bms-page-header"><div><h1>Invoices</h1><p>Manage invoices</p></div></div>
      <div class="empty-state"><mat-icon>receipt_long</mat-icon><h3>Invoices — Phase 6</h3><p>Invoice management will be built in Phase 6.</p></div>
    </div>`
})
export class InvoicesComponent {}
