import {
  Component, Inject, OnInit, OnDestroy,
} from '@angular/core';
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
import { MatDatepickerModule } from '@angular/material/datepicker';
import { MatTooltipModule } from '@angular/material/tooltip';
import { Subject, forkJoin } from 'rxjs';
import { takeUntil, finalize } from 'rxjs/operators';

import { LeaseService } from '../lease.service';
import { UnitService } from '../../units/unit.service';
import { TenantService } from '../../tenants/tenant.service';
import { LeaseDto, LEASE_STATUS_LABELS } from '../../../shared/models/lease.models';
import { UnitDto } from '../../../shared/models/unit.models';
import { TenantDto } from '../../../shared/models/tenant.models';

// ─────────────────────────────────────────────────────────────────────────────
// Standalone validators (outside class — never capture stale `this`)
// ─────────────────────────────────────────────────────────────────────────────

/**
 * Control-level validator: the Date value must be today or in the future.
 * Works with mat-datepicker which stores native Date objects.
 */
function notInPastValidator(control: AbstractControl): ValidationErrors | null {
  if (!control.value) return null;          // let `required` handle empty
  const selected = new Date(control.value);
  selected.setHours(0, 0, 0, 0);
  const today = new Date();
  today.setHours(0, 0, 0, 0);
  return selected < today ? { pastDate: true } : null;
}

/**
 * Group-level validator: endDate must be strictly after startDate.
 * Also sets / clears the `dateRange` error directly on the endDate control
 * so mat-form-field shows the error inline beneath the End Date field.
 */
function dateRangeValidator(group: AbstractControl): ValidationErrors | null {
  const start: Date | null = group.get('startDate')?.value ?? null;
  const end: Date | null   = group.get('endDate')?.value   ?? null;

  const endCtrl = group.get('endDate')!;

  if (start && end) {
    const s = new Date(start); s.setHours(0, 0, 0, 0);
    const e = new Date(end);   e.setHours(0, 0, 0, 0);

    if (e <= s) {
      // Merge into existing errors rather than overwriting them
      endCtrl.setErrors({ ...(endCtrl.errors ?? {}), dateRange: true });
      return { dateRange: true };
    }
  }

  // Clear only the dateRange error; leave any other errors intact
  if (endCtrl.hasError('dateRange')) {
    const { dateRange: _removed, ...rest } = endCtrl.errors ?? {};
    endCtrl.setErrors(Object.keys(rest).length ? rest : null);
  }
  return null;
}

// ─────────────────────────────────────────────────────────────────────────────
export interface LeaseFormDialogData { lease?: LeaseDto; }

// ─────────────────────────────────────────────────────────────────────────────
@Component({
  selector: 'app-lease-form-dialog',
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
    MatDatepickerModule,
    MatTooltipModule,
  ],
  templateUrl: './lease-form-dialog.component.html',
  styleUrls:  ['./lease-form-dialog.component.scss'],
})
export class LeaseFormDialogComponent implements OnInit, OnDestroy {

  form!:    FormGroup;
  loading          = false;
  loadingDropdowns = false;
  isEdit           = false;

  units:   UnitDto[]   = [];
  tenants: TenantDto[] = [];

  /** Minimum selectable date in the Start Date picker — today. */
  readonly minStartDate = new Date();

  /**
   * Minimum selectable date in the End Date picker.
   * Reacts to Start Date changes so the calendar greys out invalid days.
   */
  minEndDate: Date = new Date();

  readonly leaseStatusLabels = LEASE_STATUS_LABELS;

  private destroy$ = new Subject<void>();

  constructor(
    private fb:            FormBuilder,
    private leaseService:  LeaseService,
    private unitService:   UnitService,
    private tenantService: TenantService,
    private snackBar:      MatSnackBar,
    public  dialogRef:     MatDialogRef<LeaseFormDialogComponent>,
    @Inject(MAT_DIALOG_DATA) public data: LeaseFormDialogData,
  ) {}

  ngOnInit(): void {
    this.isEdit = !!this.data?.lease;
    const l = this.data?.lease;

    // Seed date fields from existing lease (edit) or leave null (create)
    const startVal = l ? this.isoToDate(l.startDate) : null;
    const endVal   = l ? this.isoToDate(l.endDate)   : null;

    // Initialise minEndDate from existing start so the calendar opens correctly
    if (startVal) this.updateMinEndDate(startVal);

    this.form = this.fb.group(
      {
        unitId:        [l?.unitId        ?? null, Validators.required],
        tenantId:      [l?.tenantId      ?? null, Validators.required],
        startDate:     [startVal, [Validators.required, notInPastValidator]],
        endDate:       [endVal,    Validators.required],
        monthlyRent:   [l?.monthlyRent   ?? null, [Validators.required, Validators.min(0.01)]],
        depositAmount: [l?.depositAmount ?? null, [Validators.required, Validators.min(0)]],
      },
      { validators: dateRangeValidator },
    );

    // Lock unit, tenant, startDate in edit/renew mode
    if (this.isEdit) {
      this.form.get('unitId')!.disable();
      this.form.get('tenantId')!.disable();
      this.form.get('startDate')!.disable();
    }

    // When startDate changes:
    //   1. Update the End Date calendar's minimum so past-start days are greyed.
    //   2. If the current endDate is now on-or-before the new startDate, clear it
    //      so the user is forced to pick a valid date.
    this.form.get('startDate')!.valueChanges
      .pipe(takeUntil(this.destroy$))
      .subscribe((newStart: Date | null) => {
        if (newStart) {
          this.updateMinEndDate(newStart);

          const currentEnd: Date | null = this.form.get('endDate')!.value;
          if (currentEnd) {
            const s = new Date(newStart); s.setHours(0, 0, 0, 0);
            const e = new Date(currentEnd); e.setHours(0, 0, 0, 0);
            if (e <= s) {
              this.form.get('endDate')!.setValue(null);
            }
          }
        }
      });

    this.loadDropdowns();
  }

  ngOnDestroy(): void {
    this.destroy$.next();
    this.destroy$.complete();
  }

  // ── Helpers ──────────────────────────────────────────────────────────────

  /** minEndDate = day after startDate so "strictly after" is enforced in UI */
  private updateMinEndDate(start: Date): void {
    const d = new Date(start);
    d.setHours(0, 0, 0, 0);
    d.setDate(d.getDate() + 1);
    this.minEndDate = d;
  }

  /** Parse an ISO string or Date into a plain Date (time stripped). */
  private isoToDate(iso: string | Date): Date {
    const d = typeof iso === 'string' ? new Date(iso) : new Date(iso);
    d.setHours(0, 0, 0, 0);
    return d;
  }

  /**
   * Converts a Date object to a full ISO-8601 UTC string for the API.
   * e.g.  Date(2025-06-01) → "2025-06-01T00:00:00.000Z"
   */
  private toIsoDateTime(d: Date): string {
    if (!d) return '';
    const yyyy = d.getFullYear();
    const mm   = String(d.getMonth() + 1).padStart(2, '0');
    const dd   = String(d.getDate()).padStart(2, '0');
    return `${yyyy}-${mm}-${dd}T00:00:00.000Z`;
  }

  private loadDropdowns(): void {
    this.loadingDropdowns = true;
    forkJoin({
      units:   this.unitService.getAll(),
      tenants: this.tenantService.getAll(),
    }).pipe(finalize(() => (this.loadingDropdowns = false)))
      .subscribe({
        next: ({ units, tenants }) => {
          this.units   = this.isEdit
            ? units
            : units.filter(u => u.status === 'Available');
          this.tenants = tenants.filter(t => t.isActive);
        },
        error: () => {
          this.snackBar.open('Failed to load units or tenants.', 'Dismiss', {
            duration: 5000, panelClass: ['snack-error'],
          });
        },
      });
  }

  // ── Convenience getters ──────────────────────────────────────────────────
  get unitId()        { return this.form.get('unitId')!;        }
  get tenantId()      { return this.form.get('tenantId')!;      }
  get startDate()     { return this.form.get('startDate')!;     }
  get endDate()       { return this.form.get('endDate')!;       }
  get monthlyRent()   { return this.form.get('monthlyRent')!;   }
  get depositAmount() { return this.form.get('depositAmount')!; }

  // ── Submit ───────────────────────────────────────────────────────────────
  onSave(): void {
    this.form.markAllAsTouched();
    if (this.form.invalid) return;

    this.loading = true;
    const v = this.form.getRawValue();   // getRawValue includes disabled controls

    if (this.isEdit) {
      const dto = {
        endDate:       this.toIsoDateTime(v.endDate),
        monthlyRent:   Number(v.monthlyRent),
        depositAmount: Number(v.depositAmount),
      };
      this.leaseService.renew(this.data.lease!.id, dto)
        .pipe(finalize(() => (this.loading = false)))
        .subscribe({
          next: lease => {
            this.snackBar.open('Lease updated successfully.', undefined, {
              duration: 3500, panelClass: ['snack-success'],
            });
            this.dialogRef.close(lease);
          },
          error: err => {
            const msg = err.error?.message ?? 'Failed to update lease.';
            this.snackBar.open(msg, 'Dismiss', { duration: 5000, panelClass: ['snack-error'] });
          },
        });
    } else {
      const dto = {
        unitId:        Number(v.unitId),
        tenantId:      Number(v.tenantId),
        startDate:     this.toIsoDateTime(v.startDate),
        endDate:       this.toIsoDateTime(v.endDate),
        monthlyRent:   Number(v.monthlyRent),
        depositAmount: Number(v.depositAmount),
      };
      this.leaseService.create(dto)
        .pipe(finalize(() => (this.loading = false)))
        .subscribe({
          next: lease => {
            this.snackBar.open('Lease created successfully.', undefined, {
              duration: 3500, panelClass: ['snack-success'],
            });
            this.dialogRef.close(lease);
          },
          error: err => {
            const msg = err.error?.message ?? 'Failed to create lease.';
            this.snackBar.open(msg, 'Dismiss', { duration: 5000, panelClass: ['snack-error'] });
          },
        });
    }
  }

  onCancel(): void { this.dialogRef.close(null); }

  statusLabel(s: string): string { return this.leaseStatusLabels[s] ?? s; }
}
