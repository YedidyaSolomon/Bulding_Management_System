import { Component, Inject, OnInit, OnDestroy } from '@angular/core';
import { CommonModule } from '@angular/common';
import {
  ReactiveFormsModule,
  FormBuilder,
  FormGroup,
  Validators,
  AbstractControl,
  ValidationErrors,
} from '@angular/forms';
import { MatDialogRef, MAT_DIALOG_DATA, MatDialogModule } from '@angular/material/dialog';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatInputModule } from '@angular/material/input';
import { MatSelectModule } from '@angular/material/select';
import { MatButtonModule } from '@angular/material/button';
import { MatIconModule } from '@angular/material/icon';
import { MatProgressSpinnerModule } from '@angular/material/progress-spinner';
import { MatSnackBar, MatSnackBarModule } from '@angular/material/snack-bar';
import { MatTooltipModule } from '@angular/material/tooltip';
import { Subject, takeUntil } from 'rxjs';
import { finalize } from 'rxjs/operators';

import { UnitService, FloorCapacityMap, MAX_FLOORS, MAX_UNITS_PER_FLOOR } from '../unit.service';
import { UnitDto, UNIT_TYPES, UNIT_STATUSES } from '../../../shared/models/unit.models';

export interface UnitFormDialogData {
  unit?:             UnitDto;
  /** Floor → existing unit count map, built from the parent's cached unit list. */
  floorCapacityMap?: FloorCapacityMap;
}

@Component({
  selector: 'app-unit-form-dialog',
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
    MatTooltipModule,
  ],
  templateUrl: './unit-form-dialog.component.html',
  styleUrls: ['./unit-form-dialog.component.scss'],
})
export class UnitFormDialogComponent implements OnInit, OnDestroy {

  form!:     FormGroup;
  loading  = false;
  isEdit   = false;

  readonly unitTypes    = UNIT_TYPES;
  readonly unitStatuses = UNIT_STATUSES;
  readonly maxFloor     = MAX_FLOORS;
  readonly maxPerFloor  = MAX_UNITS_PER_FLOOR;

  /** Capacity state for the currently-typed floor number. */
  currentFloorCount    = 0;
  currentFloorFull     = false;

  private capacityMap: FloorCapacityMap = {};
  private destroy$    = new Subject<void>();

  constructor(
    private fb:          FormBuilder,
    private unitService: UnitService,
    private snackBar:    MatSnackBar,
    public  dialogRef:   MatDialogRef<UnitFormDialogComponent>,
    @Inject(MAT_DIALOG_DATA) public data: UnitFormDialogData,
  ) {}

  ngOnInit(): void {
    this.isEdit       = !!this.data?.unit;
    this.capacityMap  = this.data?.floorCapacityMap ?? {};
    const u           = this.data?.unit;

    this.form = this.fb.group({
      unitNumber:   [u?.unitNumber   ?? '', [Validators.required, Validators.maxLength(20)]],
      floorNumber:  [
        u?.floorNumber ?? null,
        [
          Validators.required,
          Validators.min(1),
          Validators.max(MAX_FLOORS),
          this.floorCapacityValidator.bind(this),
        ],
      ],
      unitType:     [u?.unitType     ?? '', Validators.required],
      areaSqMeters: [u?.areaSqMeters ?? null, [Validators.required, Validators.min(1)]],
      monthlyRent:  [u?.monthlyRent  ?? null, [Validators.required, Validators.min(0.01)]],
      status:       [u?.status       ?? 'Available', Validators.required],
      description:  [u?.description  ?? ''],
    });

    // Status not editable on create
    if (!this.isEdit) {
      this.form.get('status')!.disable();
    }

    // Keep the capacity display in sync as the user types a floor number
    this.form.get('floorNumber')!.valueChanges
      .pipe(takeUntil(this.destroy$))
      .subscribe(val => this.updateFloorCapacityDisplay(Number(val)));

    // Seed initial display if editing
    if (u) this.updateFloorCapacityDisplay(u.floorNumber);
  }

  ngOnDestroy(): void {
    this.destroy$.next();
    this.destroy$.complete();
  }

  // ── Capacity helpers ───────────────────────────────────────────────────────

  private updateFloorCapacityDisplay(floor: number): void {
    if (!floor || floor < 1 || floor > MAX_FLOORS) {
      this.currentFloorCount = 0;
      this.currentFloorFull  = false;
      return;
    }
    this.currentFloorCount = this.capacityMap[floor] ?? 0;
    this.currentFloorFull  = this.currentFloorCount >= MAX_UNITS_PER_FLOOR;
    // Re-run validation so the error appears immediately
    this.form.get('floorNumber')!.updateValueAndValidity({ emitEvent: false });
  }

  /**
   * Custom validator — rejects the floor if it is already at capacity.
   * On an edit, the parent passes the map with the current unit excluded,
   * so moving a unit to the same floor it is already on is allowed.
   */
  private floorCapacityValidator(control: AbstractControl): ValidationErrors | null {
    const floor = Number(control.value);
    if (!floor || floor < 1 || floor > MAX_FLOORS) return null; // other validators handle this
    const count = this.capacityMap[floor] ?? 0;
    return count >= MAX_UNITS_PER_FLOOR ? { floorFull: { count, max: MAX_UNITS_PER_FLOOR } } : null;
  }

  // ── Form accessors ─────────────────────────────────────────────────────────

  get unitNumber()   { return this.form.get('unitNumber')!;   }
  get floorNumber()  { return this.form.get('floorNumber')!;  }
  get unitType()     { return this.form.get('unitType')!;     }
  get areaSqMeters() { return this.form.get('areaSqMeters')!; }
  get monthlyRent()  { return this.form.get('monthlyRent')!;  }
  get status()       { return this.form.get('status')!;       }

  /** Capacity dots: filled = occupied, empty = available */
  get capacityDots(): { filled: boolean }[] {
    return Array.from({ length: MAX_UNITS_PER_FLOOR }, (_, i) => ({
      filled: i < this.currentFloorCount,
    }));
  }

  // ── Save ───────────────────────────────────────────────────────────────────

  onSave(): void {
    if (this.form.invalid) {
      this.form.markAllAsTouched();
      return;
    }

    this.loading = true;
    const v = this.form.getRawValue();

    if (this.isEdit) {
      this.unitService.update(this.data.unit!.id, {
        floorNumber:  Number(v.floorNumber),
        unitNumber:   v.unitNumber.trim(),
        unitType:     v.unitType,
        areaSqMeters: Number(v.areaSqMeters),
        monthlyRent:  Number(v.monthlyRent),
        status:       v.status,
        description:  v.description?.trim() || null,
      })
      .pipe(finalize(() => (this.loading = false)))
      .subscribe({
        next: unit => {
          this.snackBar.open('Unit updated successfully.', undefined, {
            duration: 3500, panelClass: ['snack-success'],
          });
          this.dialogRef.close(unit);
        },
        error: err => {
          const msg = err.error?.message ?? 'Failed to update unit.';
          this.snackBar.open(msg, 'Dismiss', { duration: 5000, panelClass: ['snack-error'] });
        },
      });
    } else {
      this.unitService.create({
        floorNumber:  Number(v.floorNumber),
        unitNumber:   v.unitNumber.trim(),
        unitType:     v.unitType,
        areaSqMeters: Number(v.areaSqMeters),
        monthlyRent:  Number(v.monthlyRent),
        description:  v.description?.trim() || null,
      })
      .pipe(finalize(() => (this.loading = false)))
      .subscribe({
        next: unit => {
          this.snackBar.open('Unit created successfully.', undefined, {
            duration: 3500, panelClass: ['snack-success'],
          });
          this.dialogRef.close(unit);
        },
        error: err => {
          const msg = err.error?.message ?? 'Failed to create unit.';
          this.snackBar.open(msg, 'Dismiss', { duration: 5000, panelClass: ['snack-error'] });
        },
      });
    }
  }

  onCancel(): void {
    this.dialogRef.close(null);
  }

  statusLabel(s: string): string {
    const map: Record<string, string> = {
      Available:        'Available',
      Occupied:         'Occupied',
      UnderMaintenance: 'Under Maintenance',
      Reserved:         'Reserved',
    };
    return map[s] ?? s;
  }
}
