import { Component, Inject, OnInit } from '@angular/core';
import { CommonModule } from '@angular/common';
import { ReactiveFormsModule, FormBuilder, FormGroup, Validators } from '@angular/forms';
import { MatDialogRef, MAT_DIALOG_DATA, MatDialogModule } from '@angular/material/dialog';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatInputModule } from '@angular/material/input';
import { MatSelectModule } from '@angular/material/select';
import { MatButtonModule } from '@angular/material/button';
import { MatIconModule } from '@angular/material/icon';
import { MatProgressSpinnerModule } from '@angular/material/progress-spinner';
import { MatSnackBar, MatSnackBarModule } from '@angular/material/snack-bar';
import { finalize } from 'rxjs/operators';
import { UnitService } from '../unit.service';
import { UnitDto, UNIT_TYPES, UNIT_STATUSES } from '../../../shared/models/unit.models';

export interface UnitFormDialogData {
  unit?: UnitDto;
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
  ],
  templateUrl: './unit-form-dialog.component.html',
  styleUrls: ['./unit-form-dialog.component.scss'],
})
export class UnitFormDialogComponent implements OnInit {

  form!: FormGroup;
  loading = false;
  isEdit  = false;

  readonly unitTypes   = UNIT_TYPES;
  readonly unitStatuses = UNIT_STATUSES;

  constructor(
    private fb:          FormBuilder,
    private unitService: UnitService,
    private snackBar:    MatSnackBar,
    public  dialogRef:   MatDialogRef<UnitFormDialogComponent>,
    @Inject(MAT_DIALOG_DATA) public data: UnitFormDialogData,
  ) {}

  ngOnInit(): void {
    this.isEdit = !!this.data?.unit;
    const u = this.data?.unit;

    this.form = this.fb.group({
      unitNumber:   [u?.unitNumber   ?? '', [Validators.required, Validators.maxLength(20)]],
      floorNumber:  [u?.floorNumber  ?? null, [Validators.required, Validators.min(0), Validators.max(200)]],
      unitType:     [u?.unitType     ?? '', Validators.required],
      areaSqMeters: [u?.areaSqMeters ?? null, [Validators.required, Validators.min(1)]],
      monthlyRent:  [u?.monthlyRent  ?? null, [Validators.required, Validators.min(0.01)]],
      status:       [u?.status       ?? 'Available', Validators.required],
      description:  [u?.description  ?? ''],
    });

    // Status is not editable on create — always set to Available by backend
    if (!this.isEdit) {
      this.form.get('status')!.disable();
    }
  }

  get unitNumber()   { return this.form.get('unitNumber')!;   }
  get floorNumber()  { return this.form.get('floorNumber')!;  }
  get unitType()     { return this.form.get('unitType')!;     }
  get areaSqMeters() { return this.form.get('areaSqMeters')!; }
  get monthlyRent()  { return this.form.get('monthlyRent')!;  }
  get status()       { return this.form.get('status')!;       }

  onSave(): void {
    if (this.form.invalid) {
      this.form.markAllAsTouched();
      return;
    }

    this.loading = true;
    const v = this.form.getRawValue();

    if (this.isEdit) {
      const dto = {
        floorNumber:  Number(v.floorNumber),
        unitNumber:   v.unitNumber.trim(),
        unitType:     v.unitType,
        areaSqMeters: Number(v.areaSqMeters),
        monthlyRent:  Number(v.monthlyRent),
        status:       v.status,
        description:  v.description?.trim() || null,
      };

      this.unitService.update(this.data.unit!.id, dto)
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
      const dto = {
        floorNumber:  Number(v.floorNumber),
        unitNumber:   v.unitNumber.trim(),
        unitType:     v.unitType,
        areaSqMeters: Number(v.areaSqMeters),
        monthlyRent:  Number(v.monthlyRent),
        description:  v.description?.trim() || null,
      };

      this.unitService.create(dto)
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
