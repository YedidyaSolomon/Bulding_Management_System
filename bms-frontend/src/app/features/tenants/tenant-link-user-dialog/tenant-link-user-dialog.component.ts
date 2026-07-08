import { Component, Inject, OnInit } from '@angular/core';
import { CommonModule } from '@angular/common';
import { ReactiveFormsModule, FormBuilder, FormGroup, Validators } from '@angular/forms';
import { MatDialogRef, MAT_DIALOG_DATA, MatDialogModule } from '@angular/material/dialog';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatInputModule } from '@angular/material/input';
import { MatButtonModule } from '@angular/material/button';
import { MatIconModule } from '@angular/material/icon';
import { MatProgressSpinnerModule } from '@angular/material/progress-spinner';
import { MatSnackBar, MatSnackBarModule } from '@angular/material/snack-bar';
import { MatAutocompleteModule } from '@angular/material/autocomplete';
import { finalize } from 'rxjs/operators';

import { TenantService } from '../tenant.service';
import { TenantDto, RegisteredUserDto } from '../../../shared/models/tenant.models';

export interface TenantLinkUserDialogData {
  tenant: TenantDto;
}

@Component({
  selector: 'app-tenant-link-user-dialog',
  standalone: true,
  imports: [
    CommonModule,
    ReactiveFormsModule,
    MatDialogModule,
    MatFormFieldModule,
    MatInputModule,
    MatButtonModule,
    MatIconModule,
    MatProgressSpinnerModule,
    MatSnackBarModule,
    MatAutocompleteModule,
  ],
  templateUrl: './tenant-link-user-dialog.component.html',
  styleUrls:  ['./tenant-link-user-dialog.component.scss'],
})
export class TenantLinkUserDialogComponent implements OnInit {

  form!: FormGroup;
  loading      = false;
  loadingUsers = false;

  registeredUsers: RegisteredUserDto[] = [];
  filteredUsers:   RegisteredUserDto[] = [];

  /** Resolved user ID after a pick — this is what gets submitted. */
  selectedUser: RegisteredUserDto | null = null;

  /**
   * True when the tenant already has a linked user and the admin picked a
   * *different* user — triggers the "are you sure?" confirmation step.
   */
  showForceConfirm = false;

  readonly isRelink: boolean;

  constructor(
    private fb:            FormBuilder,
    private tenantService: TenantService,
    private snackBar:      MatSnackBar,
    public  dialogRef:     MatDialogRef<TenantLinkUserDialogComponent>,
    @Inject(MAT_DIALOG_DATA) public data: TenantLinkUserDialogData,
  ) {
    this.isRelink = !!data.tenant.appUserId;
  }

  ngOnInit(): void {
    this.form = this.fb.group({ userSearch: ['', Validators.required] });

    this.loadingUsers = true;
    this.tenantService.getRegisteredUsers()
      .pipe(finalize(() => (this.loadingUsers = false)))
      .subscribe({
        next: users => {
          this.registeredUsers = users;
          this.filteredUsers   = users;
        },
        error: () => this.snackBar.open('Failed to load user list.', 'Dismiss', { duration: 5000 }),
      });

    this.form.get('userSearch')!.valueChanges.subscribe(value => {
      // If user types after a pick, clear the resolved ID unless they retype the exact label
      if (this.selectedUser && this.displayUser(this.selectedUser) !== value) {
        this.selectedUser    = null;
        this.showForceConfirm = false;
      }
      const term = (value ?? '').toLowerCase();
      this.filteredUsers = this.registeredUsers.filter(
        u => u.email.toLowerCase().includes(term) || u.fullName.toLowerCase().includes(term)
      );
    });
  }

  onUserSelected(user: RegisteredUserDto): void {
    this.selectedUser = user;
    this.form.get('userSearch')!.setValue(this.displayUser(user), { emitEvent: false });
    this.filteredUsers = this.registeredUsers;
    // If this is a re-link to a different user, trigger the confirm step
    this.showForceConfirm =
      this.isRelink && user.id !== this.data.tenant.appUserId;
  }

  clearSelection(): void {
    this.selectedUser    = null;
    this.showForceConfirm = false;
    this.form.get('userSearch')!.setValue('');
    this.filteredUsers = this.registeredUsers;
  }

  displayUser(user: RegisteredUserDto | null): string {
    return user ? `${user.fullName} (${user.email})` : '';
  }

  /** First click: submit without force (or with force after confirm). */
  onLink(force = false): void {
    if (!this.selectedUser) {
      this.form.markAllAsTouched();
      return;
    }

    // If the tenant already has a different user linked and we haven't confirmed yet,
    // show the force-confirm banner instead of submitting.
    if (this.showForceConfirm && !force) {
      return; // template will show the confirm panel
    }

    this.loading = true;
    this.tenantService
      .linkUser(this.data.tenant.id, { appUserId: this.selectedUser.id, force })
      .pipe(finalize(() => (this.loading = false)))
      .subscribe({
        next: updated => {
          this.snackBar.open('Tenant linked to user successfully.', undefined, {
            duration: 3500, panelClass: ['snack-success'],
          });
          this.dialogRef.close(updated);
        },
        error: err => {
          const msg = err?.error?.message ?? 'Failed to link user.';
          this.snackBar.open(msg, 'Dismiss', { duration: 6000, panelClass: ['snack-error'] });
        },
      });
  }

  onCancel(): void { this.dialogRef.close(null); }
}
