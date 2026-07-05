import { Component, Inject } from '@angular/core';
import { CommonModule } from '@angular/common';
import { MatDialogRef, MAT_DIALOG_DATA, MatDialogModule } from '@angular/material/dialog';
import { MatButtonModule } from '@angular/material/button';
import { MatIconModule } from '@angular/material/icon';
import { MatDividerModule } from '@angular/material/divider';
import { UnitDto } from '../../../shared/models/unit.models';
import { EtbCurrencyPipe } from '../../../shared/pipes/etb-currency.pipe';

export interface UnitDetailDialogData {
  unit: UnitDto;
}

@Component({
  selector: 'app-unit-detail-dialog',
  standalone: true,
  imports: [
    CommonModule,
    MatDialogModule,
    MatButtonModule,
    MatIconModule,
    MatDividerModule,
    EtbCurrencyPipe,
  ],
  templateUrl: './unit-detail-dialog.component.html',
  styleUrls: ['./unit-detail-dialog.component.scss'],
})
export class UnitDetailDialogComponent {

  constructor(
    public  dialogRef: MatDialogRef<UnitDetailDialogComponent>,
    @Inject(MAT_DIALOG_DATA) public data: UnitDetailDialogData,
  ) {}

  statusLabel(s: string): string {
    const map: Record<string, string> = {
      Available:        'Available',
      Occupied:         'Occupied',
      UnderMaintenance: 'Under Maintenance',
      Reserved:         'Reserved',
    };
    return map[s] ?? s;
  }

  statusClass(s: string): string {
    const map: Record<string, string> = {
      Available:        'badge-success',
      Occupied:         'badge-info',
      UnderMaintenance: 'badge-warning',
      Reserved:         'badge-neutral',
    };
    return map[s] ?? 'badge-neutral';
  }

  close(): void {
    this.dialogRef.close();
  }
}
