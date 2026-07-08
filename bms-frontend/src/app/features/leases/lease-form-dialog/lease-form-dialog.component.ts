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
import { MatChipsModule } from '@angular/material/chips';
import { Subject } from 'rxjs';
import { takeUntil, finalize, switchMap, startWith } from 'rxjs/operators';

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
    MatChipsModule,
  ],
  templateUrl: './lease-form-dialog.component.html',
  styleUrls:  ['./lease-form-dialog.component.scss'],
})
export class LeaseFormDialogComponent implements OnInit, OnDestroy {

  form!:    FormGroup;
  loading          = false;
  loadingDropdowns = false;
  loadingUnits     = false;   // separate spinner for the reactive unit reload
  isEdit           = false;

  /** Full tenant list (active only). Loaded once at init. */
  tenants: TenantDto[] = [];

  /**
   * Unit list shown in the dropdown.
   * In create mode this is populated reactively whenever tenantId changes.
   * In edit mode it contains all units (the field is disabled anyway).
   */
  units: UnitDto[] = [];

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

    const startVal = l ? this.isoToDate(l.startDate) : null;
    const endVal   = l ? this.isoToDate(l.endDate)   : null;

    if (startVal) this.updateMinEndDate(startVal);

    this.form = this.fb.group(
      {
        tenantId:      [l?.tenantId      ?? null, Validators.required],
        unitId:        [l?.unitId        ?? null, Validators.required],
        startDate:     [startVal, [Validators.required, notInPastValidator]],
        endDate:       [endVal,    Validators.required],
        monthlyRent:   [l?.monthlyRent   ?? null, [Validators.required, Validators.min(0.01)]],
        depositAmount: [l?.depositAmount ?? null, [Validators.required, Validators.min(0)]],
      },
      { validators: dateRangeValidator },
    );

    // In edit/renew mode lock the immutable fields
    if (this.isEdit) {
      this.form.get('unitId')!.disable();
      this.form.get('tenantId')!.disable();
      this.form.get('startDate')!.disable();
    }

    // Keep endDate min in sync with startDate
    this.form.get('startDate')!.valueChanges
      .pipe(takeUntil(this.destroy$))
      .subscribe((newStart: Date | null) => {
        if (newStart) {
          this.updateMinEndDate(newStart);

          const currentEnd: Date | null = this.form.get('endDate')!.value;
          if (currentEnd) {
            const s = new Date(newStart); s.setHours(0, 0, 0, 0);
            const e = new Date(currentEnd); e.setHours(0, 0, 0, 0);
            if (e <= s) this.form.get('endDate')!.setValue(null);
          }
        }
      });

    if (this.isEdit) {
      // Edit mode: just load all units (dropdown is read-only) + tenant list
      this.loadAllDropdowns();
    } else {
      // Create mode: load tenants first, then reactively load units when a
      // tenant is selected.
      this.loadTenantsAndWireUnitDropdown();
    }
  }

  ngOnDestroy(): void {
    this.destroy$.next();
    this.destroy$.complete();
  }

  // ── Data loading ─────────────────────────────────────────────────────────

  /** Edit mode: load everything at once (unit dropdown is disabled). */
  private loadAllDropdowns(): void {
    this.loadingDropdowns = true;
    this.unitService.getAll()
      .pipe(
        finalize(() => (this.loadingDropdowns = false)),
        takeUntil(this.destroy$),
      )
      .subscribe({
        next: units => (this.units = units),
        error: () => this.showDropdownError(),
      });

    // Tenants also needed for the display value in edit mode
    this.tenantService.getAll()
      .pipe(takeUntil(this.destroy$))
      .subscribe({ next: tenants => (this.tenants = tenants.filter(t => t.isActive)) });
  }

  /**
   * Create mode:
   * 1. Load active tenants once.
   * 2. Whenever tenantId changes (including the initial null), reload the unit
   *    dropdown via the tenant-aware endpoint.
   *    — If no tenant is selected yet: clear units and disable the unit control.
   *    — If a tenant is selected: fetch selectable units, enable the unit control,
   *      and pin any reserved-for-this-tenant unit at the top.
   */
  private loadTenantsAndWireUnitDropdown(): void {
    this.loadingDropdowns = true;
    // Disable unit until a tenant is chosen
    this.form.get('unitId')!.disable();

    this.tenantService.getAll()
      .pipe(
        finalize(() => (this.loadingDropdowns = false)),
        takeUntil(this.destroy$),
      )
      .subscribe({
        next: tenants => {
          this.tenants = tenants.filter(t => t.isActive);

          // Now wire the reactive unit loader
          this.form.get('tenantId')!.valueChanges
            .pipe(
              startWith(this.form.get('tenantId')!.value as number | null),
              takeUntil(this.destroy$),
            )
            .subscribe((tenantId: number | null) => {
              const unitCtrl = this.form.get('unitId')!;

              if (!tenantId) {
                // No tenant selected — clear and disable unit
                this.units = [];
                unitCtrl.setValue(null);
                unitCtrl.disable();
                return;
              }

              // Tenant selected — fetch selectable units
              this.loadingUnits = true;
              unitCtrl.disable();

              this.unitService.getSelectableForLease(tenantId)
                .pipe(
                  finalize(() => (this.loadingUnits = false)),
                  takeUntil(this.destroy$),
                )
                .subscribe({
                  next: units => {
                    // Pin the reserved-for-this-tenant unit at the top
                    const reserved  = units.filter(u => u.isReservedForRequestedTenant);
                    const available = units.filter(u => !u.isReservedForRequestedTenant);
                    this.units = [...reserved, ...available];

                    unitCtrl.enable();

                    // Auto-select if there's exactly one reserved unit for this tenant
                    if (reserved.length === 1 && unitCtrl.value == null) {
                      unitCtrl.setValue(reserved[0].id);
                    }
                  },
                  error: () => {
                    this.units = [];
                    unitCtrl.enable();
                    this.snackBar.open('Failed to load units for this tenant.', 'Dismiss', {
                      duration: 5000, panelClass: ['snack-error'],
                    });
                  },
                });
            });
        },
        error: () => this.showDropdownError(),
      });
  }

  private showDropdownError(): void {
    this.snackBar.open('Failed to load tenants.', 'Dismiss', {
      duration: 5000, panelClass: ['snack-error'],
    });
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

  /** Converts a Date object to a full ISO-8601 UTC string for the API. */
  private toIsoDateTime(d: Date): string {
    if (!d) return '';
    const yyyy = d.getFullYear();
    const mm   = String(d.getMonth() + 1).padStart(2, '0');
    const dd   = String(d.getDate()).padStart(2, '0');
    return `${yyyy}-${mm}-${dd}T00:00:00.000Z`;
  }

  /** True when the unit dropdown should show the "Select a tenant first" hint. */
  get noTenantSelected(): boolean {
    return !this.isEdit && !this.form.get('tenantId')!.value;
  }

  /** True when the loaded unit list contains a unit reserved for the selected tenant. */
  get hasReservedUnit(): boolean {
    return this.units.some(u => u.isReservedForRequestedTenant);
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
    const v = this.form.getRawValue();   // includes disabled controls

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
