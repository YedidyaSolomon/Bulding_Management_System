import { Component, Inject, OnInit } from '@angular/core';
import { CommonModule } from '@angular/common';
import { MatDialogRef, MAT_DIALOG_DATA, MatDialogModule } from '@angular/material/dialog';
import { MatButtonModule } from '@angular/material/button';
import { MatIconModule } from '@angular/material/icon';
import { MatProgressSpinnerModule } from '@angular/material/progress-spinner';
import { MatSnackBar, MatSnackBarModule } from '@angular/material/snack-bar';
import { MatDividerModule } from '@angular/material/divider';
import { finalize } from 'rxjs/operators';

import { InvoiceService } from '../invoice.service';
import { PaymentService } from '../../payments/payment.service';
import { InvoiceDto, INVOICE_STATUS_LABELS, INVOICE_STATUS_CLASSES, MONTH_NAMES } from '../../../shared/models/invoice.models';
import { PaymentDto, PAYMENT_METHOD_LABELS } from '../../../shared/models/payment.models';
import { EtbCurrencyPipe } from '../../../shared/pipes/etb-currency.pipe';

export interface InvoiceDetailDialogData {
  invoice:   InvoiceDto;
  canManage: boolean;
  canCancel: boolean;
}

@Component({
  selector: 'app-invoice-detail-dialog',
  standalone: true,
  imports: [
    CommonModule,
    MatDialogModule,
    MatButtonModule,
    MatIconModule,
    MatProgressSpinnerModule,
    MatSnackBarModule,
    MatDividerModule,
    EtbCurrencyPipe,
  ],
  templateUrl: './invoice-detail-dialog.component.html',
  styleUrls: ['./invoice-detail-dialog.component.scss'],
})
export class InvoiceDetailDialogComponent implements OnInit {

  invoice:  InvoiceDto;
  payments: PaymentDto[] = [];

  loadingPayments = false;
  actionLoading   = false;

  readonly methodLabels = PAYMENT_METHOD_LABELS;

  constructor(
    private invoiceService: InvoiceService,
    private paymentService: PaymentService,
    private snackBar:       MatSnackBar,
    public  dialogRef:      MatDialogRef<InvoiceDetailDialogComponent>,
    @Inject(MAT_DIALOG_DATA) public data: InvoiceDetailDialogData,
  ) {
    this.invoice = data.invoice;
  }

  ngOnInit(): void { this.loadPayments(); }

  private loadPayments(): void {
    this.loadingPayments = true;
    this.paymentService.getByInvoice(this.invoice.id)
      .pipe(finalize(() => (this.loadingPayments = false)))
      .subscribe({ next: p => (this.payments = p), error: () => {} });
  }

  statusLabel(s: string): string { return INVOICE_STATUS_LABELS[s]  ?? s; }
  statusClass(s: string): string { return INVOICE_STATUS_CLASSES[s] ?? 'badge-neutral'; }
  methodLabel(s: string): string { return this.methodLabels[s] ?? s; }

  get periodLabel(): string {
    return `${MONTH_NAMES[this.invoice.periodMonth - 1]} ${this.invoice.periodYear}`;
  }

  get totalPaid(): number {
    return this.payments.reduce((sum, p) => sum + p.amountPaid, 0);
  }

  get balance(): number {
    return this.invoice.amountDue - this.totalPaid;
  }

  get canIssue():    boolean { return this.data.canManage && this.invoice.status === 'Draft'; }
  get canCancelInv():boolean { return this.data.canCancel && this.invoice.status !== 'Paid' && this.invoice.status !== 'Cancelled'; }

  onIssue(): void {
    this.actionLoading = true;
    this.invoiceService.issue(this.invoice.id)
      .pipe(finalize(() => (this.actionLoading = false)))
      .subscribe({
        next: updated => {
          this.invoice = updated;
          this.snackBar.open('Invoice issued.', undefined, { duration: 3000, panelClass: ['snack-success'] });
          this.dialogRef.close(updated);
        },
        error: err => {
          this.snackBar.open(err.error?.message ?? 'Failed to issue invoice.', 'Dismiss', { duration: 5000, panelClass: ['snack-error'] });
        },
      });
  }

  onCancel(): void {
    this.actionLoading = true;
    this.invoiceService.cancel(this.invoice.id)
      .pipe(finalize(() => (this.actionLoading = false)))
      .subscribe({
        next: updated => {
          this.invoice = updated;
          this.snackBar.open('Invoice cancelled.', undefined, { duration: 3000, panelClass: ['snack-success'] });
          this.dialogRef.close(updated);
        },
        error: err => {
          this.snackBar.open(err.error?.message ?? 'Failed to cancel invoice.', 'Dismiss', { duration: 5000, panelClass: ['snack-error'] });
        },
      });
  }

  onClose(): void { this.dialogRef.close(null); }
}
