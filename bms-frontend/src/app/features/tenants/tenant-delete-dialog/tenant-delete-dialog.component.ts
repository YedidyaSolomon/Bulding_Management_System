import { Component, Inject } from '@angular/core';
import { CommonModule } from '@angular/common';
import { MatDialogRef, MAT_DIALOG_DATA, MatDialogModule } from '@angular/material/dialog';
import { MatButtonModule } from '@angular/material/button';
import { MatIconModule } from '@angular/material/icon';
import { MatProgressSpinnerModule } from '@angular/material/progress-spinner';
import { MatSnackBar, MatSnackBarModule } from '@angular/material/snack-bar';
import { finalize } from 'rxjs/operators';

import { TenantService } from '../tenant.service';
import { TenantDto } from '../../../shared/models/tenant.models';

export interface TenantDeleteDialogData {
  tenant: TenantDto;
}

@Component({
  selector: 'app-tenant-delete-dialog',
  standalone: true,
  imports: [
    CommonModule,
    MatDialogModule,
    MatButtonModule,
    MatIconModule,
    MatProgressSpinnerModule,
    MatSnackBarModule,
  ],
  templateUrl: './tenant-delete-dialog.component.html',
  styleUrls: ['./tenant-delete-dialog.component.scss'],
})
export class TenantDeleteDialogComponent {

  loading = false;

  constructor(
    private tenantService: TenantService,
    private snackBar:      MatSnackBar,
    public  dialogRef:     MatDialogRef<TenantDeleteDialogComponent>,
    @Inject(MAT_DIALOG_DATA) public data: TenantDeleteDialogData,
  ) {}

  onConfirm(): void {
    this.loading = true;
    this.tenantService.delete(this.data.tenant.id)
      .pipe(finalize(() => (this.loading = false)))
      .subscribe({
        next: () => {
          this.snackBar.open('Tenant deactivated successfully.', undefined, {
            duration: 3500, panelClass: ['snack-success'],
          });
          this.dialogRef.close(true);
        },
        error: err => {
          const msg = err.error?.message ?? 'Failed to deactivate tenant.';
          this.snackBar.open(msg, 'Dismiss', { duration: 6000, panelClass: ['snack-error'] });
          this.dialogRef.close(false);
        },
      });
  }

  onCancel(): void {
    this.dialogRef.close(false);
  }
}
