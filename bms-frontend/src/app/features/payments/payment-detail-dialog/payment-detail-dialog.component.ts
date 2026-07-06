import { Component, Inject } from '@angular/core';
import { CommonModule } from '@angular/common';
import { MatDialogRef, MAT_DIALOG_DATA, MatDialogModule } from '@angular/material/dialog';
import { MatButtonModule } from '@angular/material/button';
import { MatIconModule } from '@angular/material/icon';
import { MatDividerModule } from '@angular/material/divider';

import { PaymentDto, PAYMENT_METHOD_LABELS, PAYMENT_METHOD_ICONS } from '../../../shared/models/payment.models';
import { EtbCurrencyPipe } from '../../../shared/pipes/etb-currency.pipe';

export interface PaymentDetailDialogData {
  payment: PaymentDto;
}

@Component({
  selector: 'app-payment-detail-dialog',
  standalone: true,
  imports: [
    CommonModule,
    MatDialogModule,
    MatButtonModule,
    MatIconModule,
    MatDividerModule,
    EtbCurrencyPipe,
  ],
  templateUrl: './payment-detail-dialog.component.html',
  styleUrls: ['./payment-detail-dialog.component.scss'],
})
export class PaymentDetailDialogComponent {

  readonly payment: PaymentDto;

  constructor(
    public  dialogRef: MatDialogRef<PaymentDetailDialogComponent>,
    @Inject(MAT_DIALOG_DATA) public data: PaymentDetailDialogData,
  ) {
    this.payment = data.payment;
  }

  methodLabel(m: string): string { return PAYMENT_METHOD_LABELS[m] ?? m; }
  methodIcon(m: string):  string { return PAYMENT_METHOD_ICONS[m]  ?? 'payments'; }

  onClose(): void { this.dialogRef.close(); }
}
