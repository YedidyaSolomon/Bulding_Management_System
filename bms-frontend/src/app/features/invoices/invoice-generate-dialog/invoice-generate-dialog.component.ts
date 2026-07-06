import { Component, OnInit } from '@angular/core';
import { CommonModule } from '@angular/common';
import {
  ReactiveFormsModule, FormBuilder, FormGroup,
  Validators, AbstractControl, ValidationErrors,
} from '@angular/forms';
import { MatDialogRef, MatDialogModule } from '@angular/material/dialog';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatInputModule } from '@angular/material/input';
import { MatSelectModule } from '@angular/material/select';
import { MatButtonModule } from '@angular/material/button';
import { MatIconModule } from '@angular/material/icon';
import { MatProgressSpinnerModule } from '@angular/material/progress-spinner';
import { MatSnackBar, MatSnackBarModule } from '@angular/material/snack-bar';
import { finalize } from 'rxjs/operators';

import { InvoiceService } from '../invoice.service';
import { LeaseService } from '../../leases/lease.service';
import { LeaseDto } from '../../../shared/models/lease.models';
import { MONTH_NAMES } from '../../../shared/models/invoice.models';
import {
  AppDatePickerComponent,
  dateToIso,
} from '../../../shared/components/date-picker/date-picker.component';

// ── Validators ────────────────────────────────────────────────────────────────

/**
 * Control-level: selected date must be today or later.
 * Works with mat-datepicker (stores Date objects).
 */
function notInPastValidator(ctrl: AbstractControl): ValidationErrors | null {
  if (!ctrl.value) return null;
  const sel = new Date(ctrl.value); sel.setHours(0, 0, 0, 0);
  const now = new Date();           now.setHours(0, 0, 0, 0);
  return sel < now ? { pastDate: true } : null;
}

// ─────────────────────────────────────────────────────────────────────────────

@Component({
  selector: 'app-invoice-generate-dialog',
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
  templateUrl: './invoice-generate-dialog.component.html',
  styleUrls: ['./invoice-generate-dialog.component.scss'],
})
export class InvoiceGenerateDialogComponent implements OnInit {

  form!:         FormGroup;
  loading        = false;
  loadingLeases  = false;

  leases:   LeaseDto[] = [];
  months    = MONTH_NAMES.map((name, i) => ({ value: i + 1, label: name }));
  years:    number[]   = [];

  /** Due Date must be ≥ today (passed to the picker's [minDate]) */
  readonly minDueDate = new Date();

  constructor(
    private fb:             FormBuilder,
    private invoiceService: InvoiceService,
    private leaseService:   LeaseService,
    private snackBar:       MatSnackBar,
    public  dialogRef:      MatDialogRef<InvoiceGenerateDialogComponent>,
  ) {}

  ngOnInit(): void {
    const now = new Date();
    const yr  = now.getFullYear();
    this.years = [yr - 1, yr, yr + 1];

    this.form = this.fb.group({
      leaseId:     [null, Validators.required],
      amountDue:   [null, [Validators.required, Validators.min(0.01)]],
      // dueDate stores a Date object (mat-datepicker)
      dueDate:     [null, [Validators.required, notInPastValidator]],
      periodMonth: [now.getMonth() + 1, Validators.required],
      periodYear:  [yr,                 Validators.required],
    });

    this.form.get('leaseId')!.valueChanges.subscribe(id => {
      const lease = this.leases.find(l => l.id === id);
      if (lease) this.form.get('amountDue')!.setValue(lease.monthlyRent);
    });

    this.loadLeases();
  }

  private loadLeases(): void {
    this.loadingLeases = true;
    this.leaseService.getAll()
      .pipe(finalize(() => (this.loadingLeases = false)))
      .subscribe({
        next: leases => {
          this.leases = leases.filter(l =>
            l.status === 'Active' || l.status === 'PendingRenewal');
        },
        error: () => {
          this.snackBar.open('Failed to load leases.', 'Dismiss', {
            duration: 5000, panelClass: ['snack-error'],
          });
        },
      });
  }

  get leaseId()     { return this.form.get('leaseId')!;     }
  get amountDue()   { return this.form.get('amountDue')!;   }
  get dueDate()     { return this.form.get('dueDate')!;     }
  get periodMonth() { return this.form.get('periodMonth')!; }
  get periodYear()  { return this.form.get('periodYear')!;  }

  onGenerate(): void {
    this.form.markAllAsTouched();
    if (this.form.invalid) return;

    this.loading = true;
    const v = this.form.value;

    const dto = {
      leaseId:     Number(v.leaseId),
      amountDue:   Number(v.amountDue),
      dueDate:     dateToIso(v.dueDate),   // Date → ISO string
      periodMonth: Number(v.periodMonth),
      periodYear:  Number(v.periodYear),
    };

    this.invoiceService.generate(dto)
      .pipe(finalize(() => (this.loading = false)))
      .subscribe({
        next: invoice => {
          this.snackBar.open(`Invoice ${invoice.invoiceNumber} generated.`, undefined, {
            duration: 3500, panelClass: ['snack-success'],
          });
          this.dialogRef.close(invoice);
        },
        error: err => {
          const msg = err.error?.message ?? 'Failed to generate invoice.';
          this.snackBar.open(msg, 'Dismiss', { duration: 5000, panelClass: ['snack-error'] });
        },
      });
  }

  onCancel(): void { this.dialogRef.close(null); }
}
