import { Component, Inject } from '@angular/core';
import { CommonModule } from '@angular/common';
import { MatDialogRef, MAT_DIALOG_DATA, MatDialogModule } from '@angular/material/dialog';
import { MatButtonModule } from '@angular/material/button';
import { MatIconModule } from '@angular/material/icon';
import { MatDividerModule } from '@angular/material/divider';

import { LeaseDto, LEASE_STATUS_LABELS, LEASE_STATUS_CLASSES } from '../../../shared/models/lease.models';
import { EtbCurrencyPipe } from '../../../shared/pipes/etb-currency.pipe';

export interface LeaseDetailDialogData {
  lease: LeaseDto;
}

@Component({
  selector: 'app-lease-detail-dialog',
  standalone: true,
  imports: [
    CommonModule,
    MatDialogModule,
    MatButtonModule,
    MatIconModule,
    MatDividerModule,
    EtbCurrencyPipe,
  ],
  templateUrl: './lease-detail-dialog.component.html',
  styleUrls: ['./lease-detail-dialog.component.scss'],
})
export class LeaseDetailDialogComponent {

  constructor(
    public  dialogRef: MatDialogRef<LeaseDetailDialogComponent>,
    @Inject(MAT_DIALOG_DATA) public data: LeaseDetailDialogData,
  ) {}

  statusLabel(s: string): string  { return LEASE_STATUS_LABELS[s]  ?? s; }
  statusClass(s: string): string  { return LEASE_STATUS_CLASSES[s] ?? 'badge-neutral'; }

  /** How many days remain until end date (negative = already ended) */
  get daysRemaining(): number {
    const end  = new Date(this.data.lease.endDate);
    const now  = new Date();
    return Math.ceil((end.getTime() - now.getTime()) / (1000 * 60 * 60 * 24));
  }

  get daysRemainingLabel(): string {
    const d = this.daysRemaining;
    if (d < 0)  return `Ended ${Math.abs(d)} day${Math.abs(d) !== 1 ? 's' : ''} ago`;
    if (d === 0) return 'Ends today';
    if (d <= 30) return `${d} day${d !== 1 ? 's' : ''} remaining`;
    return `${d} days remaining`;
  }

  get daysRemainingClass(): string {
    const d = this.daysRemaining;
    if (d < 0)   return 'days-expired';
    if (d <= 30) return 'days-warning';
    return 'days-ok';
  }

  /** Total lease duration in months (rounded) */
  get leaseDurationMonths(): number {
    const start = new Date(this.data.lease.startDate);
    const end   = new Date(this.data.lease.endDate);
    return Math.round(
      (end.getFullYear() - start.getFullYear()) * 12 +
      (end.getMonth()   - start.getMonth())
    );
  }

  close(): void { this.dialogRef.close(); }
}
