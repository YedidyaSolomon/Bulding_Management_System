import { Component, Inject, OnInit } from '@angular/core';
import { CommonModule } from '@angular/common';
import {
  ReactiveFormsModule, FormBuilder, FormGroup,
  Validators, AbstractControl, ValidationErrors,
} from '@angular/forms';
import { MatDialogRef, MAT_DIALOG_DATA, MatDialogModule } from '@angular/material/dialog';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatInputModule } from '@angular/material/input';
import { MatSelectModule } from '@angular/material/select';
import { MatButtonModule } from '@angular/material/button';
import { MatIconModule } from '@angular/material/icon';
import { MatProgressSpinnerModule } from '@angular/material/progress-spinner';
import { MatSnackBar, MatSnackBarModule } from '@angular/material/snack-bar';
import { finalize } from 'rxjs/operators';

import { PaymentService } from '../payment.service';
import { InvoiceDto } from '../../../shared/models/invoice.models';
import { PAYMENT_METHODS, PAYMENT_METHOD_LABELS } from '../../../shared/models/payment.models';
import {
  AppDatePickerComponent,
  dateToIso,
} from '../../../shared/components/date-picker/date-picker.component';

export interface PaymentRecordDialogData {
  invoice:  InvoiceDto;
  invoices: InvoiceDto[];   // full list so user can switch invoice inside the dialog
}

// ── Validator ─────────────────────────────────────────────────────────────────

/**
 * Control-level: payment date must not be in the future —
 * you cannot record a payment that hasn't happened yet.
 */
function notFutureValidator(ctrl: AbstractControl): ValidationErrors | null {
  if (!ctrl.value) return null;
  const sel = new Date(ctrl.value); sel.setHours(0, 0, 0, 0);
  const now = new Date();           now.setHours(0, 0, 0, 0);
  return sel > now ? { futureDate: true } : null;
}

// ─────────────────────────────────────────────────────────────────────────────

@Component({
  selector: 'app-payment-record-dialog',
  standalone: true,
  imports: [
    CommonModule,
    ReactiveFormsModule,
    MatDialogModule,
    MatFormFieldModule,
    MatInputModule,
    MatSelectModule,
    MatButtonModule,
    MatIconModule,
    MatProgressSpinnerModule,
    MatSnackBarModule,
    AppDatePickerComponent,
  ],
  templateUrl: './payment-record-dialog.component.html',
  styleUrls: ['./payment-record-dialog.component.scss'],
})
export class PaymentRecordDialogComponent implements OnInit {

  form!:   FormGroup;
  loading = false;

  readonly paymentMethods      = PAYMENT_METHODS;
  readonly paymentMethodLabels = PAYMENT_METHOD_LABELS;

  /** Payment date must not exceed today — passed to [maxDate] on the picker. */
  readonly maxPaymentDate = new Date();

  constructor(
    private fb:             FormBuilder,
    private paymentService: PaymentService,
    private snackBar:       MatSnackBar,
    public  dialogRef:      MatDialogRef<PaymentRecordDialogComponent>,
    @Inject(MAT_DIALOG_DATA) public data: PaymentRecordDialogData,
  ) {}

  ngOnInit(): void {
    // Default to today as a Date object (mat-datepicker requires Date, not string)
    const today = new Date();
    today.setHours(0, 0, 0, 0);

    this.form = this.fb.group({
      invoiceId:       [this.data.invoice.id,       Validators.required],
      amountPaid:      [this.data.invoice.amountDue, [Validators.required, Validators.min(0.01)]],
      paymentDate:     [today,  [Validators.required, notFutureValidator]],
      paymentMethod:   ['Cash', Validators.required],
      referenceNumber: [''],
      notes:           [''],
    });

    // When the user picks a different invoice, pre-fill amountPaid with its amountDue
    this.form.get('invoiceId')!.valueChanges.subscribe((id: number) => {
      const selected = this.data.invoices.find(i => i.id === id);
      if (selected) {
        this.form.get('amountPaid')!.setValue(selected.amountDue, { emitEvent: false });
      }
    });
  }

  get invoiceId()       { return this.form.get('invoiceId')!;       }
  get amountPaid()      { return this.form.get('amountPaid')!;       }
  get paymentDate()     { return this.form.get('paymentDate')!;      }
  get paymentMethod()   { return this.form.get('paymentMethod')!;    }
  get referenceNumber() { return this.form.get('referenceNumber')!;  }

  /** Currently selected invoice (for the hint text) */
  get selectedInvoice(): InvoiceDto | undefined {
    const id = this.form.get('invoiceId')!.value;
    return this.data.invoices.find(i => i.id === id);
  }

  methodLabel(m: string): string { return this.paymentMethodLabels[m] ?? m; }

  onRecord(): void {
    this.form.markAllAsTouched();
    if (this.form.invalid) return;

    this.loading = true;
    const v = this.form.value;

    const dto = {
      invoiceId:       Number(v.invoiceId),
      amountPaid:      Number(v.amountPaid),
      paymentDate:     dateToIso(v.paymentDate),   // Date → ISO string
      paymentMethod:   v.paymentMethod,
      referenceNumber: v.referenceNumber?.trim() ?? '',
      notes:           v.notes?.trim() || null,
    };

    this.paymentService.record(dto)
      .pipe(finalize(() => (this.loading = false)))
      .subscribe({
        next: payment => {
          this.snackBar.open('Payment recorded successfully.', undefined, {
            duration: 3500, panelClass: ['snack-success'],
          });
          this.dialogRef.close(payment);
        },
        error: err => {
          const msg = err.error?.message ?? 'Failed to record payment.';
          this.snackBar.open(msg, 'Dismiss', { duration: 5000, panelClass: ['snack-error'] });
        },
      });
  }

  onCancel(): void { this.dialogRef.close(null); }
}
