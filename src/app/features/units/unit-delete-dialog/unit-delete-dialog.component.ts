import { Component, Inject } from '@angular/core';
import { CommonModule } from '@angular/common';
import { MatDialogRef, MAT_DIALOG_DATA, MatDialogModule } from '@angular/material/dialog';
import { MatButtonModule } from '@angular/material/button';
import { MatIconModule } from '@angular/material/icon';
import { MatProgressSpinnerModule } from '@angular/material/progress-spinner';
import { MatSnackBar, MatSnackBarModule } from '@angular/material/snack-bar';
import { finalize } from 'rxjs/operators';
import { UnitService } from '../unit.service';
import { UnitDto } from '../../../shared/models/unit.models';

export interface UnitDeleteDialogData {
  unit: UnitDto;
}

@Component({
  selector: 'app-unit-delete-dialog',
  standalone: true,
  imports: [
    CommonModule,
    MatDialogModule,
    MatButtonModule,
    MatIconModule,
    MatProgressSpinnerModule,
    MatSnackBarModule,
  ],
  templateUrl: './unit-delete-dialog.component.html',
  styleUrls: ['./unit-delete-dialog.component.scss'],
})
export class UnitDeleteDialogComponent {

  loading = false;

  constructor(
    private unitService: UnitService,
    private snackBar:    MatSnackBar,
    public  dialogRef:   MatDialogRef<UnitDeleteDialogComponent>,
    @Inject(MAT_DIALOG_DATA) public data: UnitDeleteDialogData,
  ) {}

  onConfirm(): void {
    this.loading = true;
    this.unitService.delete(this.data.unit.id)
      .pipe(finalize(() => (this.loading = false)))
      .subscribe({
        next: () => {
          this.snackBar.open('Unit deleted successfully.', undefined, {
            duration: 3500, panelClass: ['snack-success'],
          });
          this.dialogRef.close(true);
        },
        error: err => {
          const msg = err.error?.message ?? 'Failed to delete unit.';
          this.snackBar.open(msg, 'Dismiss', { duration: 6000, panelClass: ['snack-error'] });
          this.dialogRef.close(false);
        },
      });
  }

  onCancel(): void {
    this.dialogRef.close(false);
  }
}
