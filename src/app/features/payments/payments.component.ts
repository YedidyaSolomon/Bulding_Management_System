import { Component } from '@angular/core';
import { CommonModule } from '@angular/common';
import { MatIconModule } from '@angular/material/icon';

@Component({
  selector: 'app-payments',
  standalone: true,
  imports: [CommonModule, MatIconModule],
  template: `
    <div class="bms-page fade-in">
      <div class="bms-page-header"><div><h1>Payments</h1><p>Track payments</p></div></div>
      <div class="empty-state"><mat-icon>payments</mat-icon><h3>Payments — Phase 7</h3><p>Payment management will be built in Phase 7.</p></div>
    </div>`
})
export class PaymentsComponent {}
