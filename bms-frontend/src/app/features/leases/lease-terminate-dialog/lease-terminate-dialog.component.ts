import { Component, Inject } from '@angular/core';
import { CommonModule } from '@angular/common';
import { ReactiveFormsModule, FormBuilder, FormGroup, Validators } from '@angular/forms';
import { MatDialogRef, MAT_DIALOG_DATA, MatDialogModule } from '@angular/material/dialog';
import { MatButtonModule } from '@angular/material/button';
import { MatIconModule } from '@angular/material/icon';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatInputModule } from '@angular/material/input';
import { MatProgressSpinnerModule } from '@angular/material/progress-spinner';
import { MatSnackBar, MatSnackBarModule } from '@angular/material/snack-bar';
import { finalize } from 'rxjs/operators';

import { LeaseService } from '../lease.service';
import { LeaseDto } from '../../../shared/models/lease.models';

export interface LeaseTerminateDialogData {
  lease: LeaseDto;
}

@Component({
  selector: 'app-lease-terminate-dialog',
  standalone: true,
  imports: [
    CommonModule,
    ReactiveFormsModule,
    MatDialogModule,
    MatButtonModule,
    MatIconModule,
    MatFormFieldModule,
    MatInputModule,
    MatProgressSpinnerModule,
    MatSnackBarModule,
  ],
  templateUrl: './lease-terminate-dialog.component.html',
  styleUrls: ['./lease-terminate-dialog.component.scss'],
})
export class LeaseTerminateDialogComponent {

  loading = false;
  form: FormGroup;

  constructor(
    private leaseService: LeaseService,
    private snackBar:     MatSnackBar,
    private fb:           FormBuilder,
    public  dialogRef:    MatDialogRef<LeaseTerminateDialogComponent>,
    @Inject(MAT_DIALOG_DATA) public data: LeaseTerminateDialogData,
  ) {
    this.form = this.fb.group({
      reason: ['', [Validators.required, Validators.minLength(5), Validators.maxLength(300)]],
    });
  }

  get reason() { return this.form.get('reason')!; }

  onConfirm(): void {
    if (this.form.invalid) {
      this.form.markAllAsTouched();
      return;
    }

    this.loading = true;
    this.leaseService.terminate(this.data.lease.id, { reason: this.reason.value.trim() })
      .pipe(finalize(() => (this.loading = false)))
      .subscribe({
        next: () => {
          this.snackBar.open('Lease terminated successfully.', undefined, {
            duration: 3500, panelClass: ['snack-success'],
          });
          this.dialogRef.close(true);
        },
        error: err => {
          const msg = err.error?.message ?? 'Failed to terminate lease.';
          this.snackBar.open(msg, 'Dismiss', { duration: 6000, panelClass: ['snack-error'] });
          this.dialogRef.close(false);
        },
      });
  }

  onCancel(): void {
    this.dialogRef.close(false);
  }
}
