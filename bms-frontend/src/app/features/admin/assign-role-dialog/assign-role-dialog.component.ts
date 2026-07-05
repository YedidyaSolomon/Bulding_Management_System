import { Component, Inject, OnInit } from '@angular/core';
import { CommonModule } from '@angular/common';
import { ReactiveFormsModule, FormBuilder, FormGroup, Validators } from '@angular/forms';
import { MatDialogRef, MAT_DIALOG_DATA, MatDialogModule } from '@angular/material/dialog';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatSelectModule } from '@angular/material/select';
import { MatButtonModule } from '@angular/material/button';
import { MatIconModule } from '@angular/material/icon';
import { MatProgressSpinnerModule } from '@angular/material/progress-spinner';
import { MatSnackBar, MatSnackBarModule } from '@angular/material/snack-bar';
import { HttpClient } from '@angular/common/http';
import { finalize } from 'rxjs/operators';
import { environment } from '../../../../environments/environment';
import { ApiResponse } from '../../../shared/models/auth.models';
import { UserListItem } from '../user-management/user-management.component';

@Component({
  selector: 'app-assign-role-dialog',
  standalone: true,
  imports: [
    CommonModule,
    ReactiveFormsModule,
    MatDialogModule,
    MatFormFieldModule,
    MatSelectModule,
    MatButtonModule,
    MatIconModule,
    MatProgressSpinnerModule,
    MatSnackBarModule,
  ],
  templateUrl: './assign-role-dialog.component.html',
  styleUrls: ['./assign-role-dialog.component.scss'],
})
export class AssignRoleDialogComponent implements OnInit {
  form!: FormGroup;
  loading = false;

  /** Only Manager and Viewer can be assigned via the API */
  readonly assignableRoles = ['Manager', 'Viewer'] as const;

  constructor(
    private fb: FormBuilder,
    private http: HttpClient,
    private snackBar: MatSnackBar,
    public  dialogRef: MatDialogRef<AssignRoleDialogComponent>,
    @Inject(MAT_DIALOG_DATA) public user: UserListItem,
  ) {}

  ngOnInit(): void {
    this.form = this.fb.group({
      role: [this.user.role, Validators.required],
    });
  }

  onSave(): void {
    if (this.form.invalid) return;

    const newRole = this.form.value.role;
    if (newRole === this.user.role) {
      this.dialogRef.close(false);
      return;
    }

    this.loading = true;
    this.http.post<ApiResponse<unknown>>(`${environment.apiUrl}/auth/assign-role`, {
      userId: this.user.id,
      role:   newRole,
    })
    .pipe(finalize(() => (this.loading = false)))
    .subscribe({
      next: () => {
        this.snackBar.open(`Role updated to ${newRole}.`, undefined, {
          duration: 3000,
          panelClass: ['snack-success'],
        });
        this.dialogRef.close(true);
      },
      error: err => {
        const msg = err.error?.message ?? 'Failed to update role.';
        this.snackBar.open(msg, 'Dismiss', { duration: 5000, panelClass: ['snack-error'] });
      },
    });
  }

  onCancel(): void {
    this.dialogRef.close(false);
  }
}
