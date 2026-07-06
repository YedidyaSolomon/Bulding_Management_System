import { Component, Input, Output, EventEmitter } from '@angular/core';
import { CommonModule } from '@angular/common';
import { MatButtonModule } from '@angular/material/button';
import { MatIconModule } from '@angular/material/icon';
import { MatTooltipModule } from '@angular/material/tooltip';

import { LeaseDto, LEASE_STATUS_LABELS, LEASE_STATUS_CLASSES } from '../../../shared/models/lease.models';
import { EtbCurrencyPipe } from '../../../shared/pipes/etb-currency.pipe';

@Component({
  selector: 'app-lease-card',
  standalone: true,
  imports: [CommonModule, MatButtonModule, MatIconModule, MatTooltipModule, EtbCurrencyPipe],
  templateUrl: './lease-card.component.html',
  styleUrls: ['./lease-card.component.scss'],
})
export class LeaseCardComponent {
  @Input() lease!:     LeaseDto;
  @Input() canEdit   = false;
  @Input() canTerminate = false;

  @Output() view      = new EventEmitter<LeaseDto>();
  @Output() edit      = new EventEmitter<LeaseDto>();
  @Output() terminate = new EventEmitter<LeaseDto>();

  statusLabel(s: string): string { return LEASE_STATUS_LABELS[s]  ?? s; }
  statusClass(s: string): string { return LEASE_STATUS_CLASSES[s] ?? 'badge-neutral'; }

  get daysRemaining(): number {
    const end = new Date(this.lease.endDate);
    const now = new Date();
    return Math.ceil((end.getTime() - now.getTime()) / (1000 * 60 * 60 * 24));
  }

  get daysLabel(): string {
    const d = this.daysRemaining;
    if (d < 0)   return `Expired`;
    if (d === 0) return 'Ends today';
    if (d <= 30) return `${d}d left`;
    return `${d}d left`;
  }

  get daysClass(): string {
    const d = this.daysRemaining;
    if (d < 0)   return 'chip-expired';
    if (d <= 30) return 'chip-warning';
    return 'chip-ok';
  }

  get isTerminable(): boolean {
    return this.lease.status === 'Active' || this.lease.status === 'PendingRenewal';
  }
}
