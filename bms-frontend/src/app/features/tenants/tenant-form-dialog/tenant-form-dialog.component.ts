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
import { MatSlideToggleModule } from '@angular/material/slide-toggle';
import { MatSnackBar, MatSnackBarModule } from '@angular/material/snack-bar';
import { MatAutocompleteModule } from '@angular/material/autocomplete';
import { finalize } from 'rxjs/operators';

import { TenantService } from '../tenant.service';
import { TenantDto, RegisteredUserDto, BUSINESS_TYPES } from '../../../shared/models/tenant.models';

export interface TenantFormDialogData {
  tenant?: TenantDto;
}

@Component({
  selector: 'app-tenant-form-dialog',
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
    MatSlideToggleModule,
    MatSnackBarModule,
    MatAutocompleteModule,
  ],
  templateUrl: './tenant-form-dialog.component.html',
  styleUrls: ['./tenant-form-dialog.component.scss'],
})
export class TenantFormDialogComponent implements OnInit {

  form!: FormGroup;
  loading = false;
  isEdit  = false;

  readonly businessTypes = BUSINESS_TYPES;

  /** Full list of registered users, used for the create-mode autocomplete */
  registeredUsers:   RegisteredUserDto[] = [];
  filteredUsers:     RegisteredUserDto[] = [];

  constructor(
    private fb:            FormBuilder,
    private tenantService: TenantService,
    private snackBar:      MatSnackBar,
    public  dialogRef:     MatDialogRef<TenantFormDialogComponent>,
    @Inject(MAT_DIALOG_DATA) public data: TenantFormDialogData,
  ) {}

  ngOnInit(): void {
    this.isEdit = !!this.data?.tenant;
    const t = this.data?.tenant;

    this.form = this.fb.group({
      userEmail:         [''                          , this.isEdit ? [] : [Validators.required, Validators.email]],
      organizationName:  [t?.organizationName  ?? '', [Validators.required, Validators.maxLength(150)]],
      tin:               [t?.tin               ?? '', [Validators.required, Validators.maxLength(30)]],
      phone:             [t?.phone             ?? '', [Validators.required, Validators.maxLength(20)]],
      businessType:      [t?.businessType      ?? '', Validators.required],
      contactPersonName: [t?.contactPersonName ?? '', [Validators.required, Validators.maxLength(100)]],
      contactEmail:      [t?.contactEmail      ?? '', [Validators.required, Validators.email, Validators.maxLength(150)]],
      isActive:          [t?.isActive          ?? true],
    });

    // isActive toggle only makes sense when editing
    if (!this.isEdit) {
      this.form.get('isActive')!.disable();
    }

    // Load registered users for the email picker (create mode only)
    if (!this.isEdit) {
      this.tenantService.getRegisteredUsers().subscribe({
        next: users => {
          this.registeredUsers = users;
          this.filteredUsers   = users;
        },
      });

      // Filter autocomplete options as the user types
      this.form.get('userEmail')!.valueChanges.subscribe(value => {
        const term = (value ?? '').toLowerCase();
        this.filteredUsers = this.registeredUsers.filter(
          u => u.email.toLowerCase().includes(term) || u.fullName.toLowerCase().includes(term)
        );
      });
    }
  }

  // ── Convenience getters ──────────────────────────────────────────────────
  get userEmail()         { return this.form.get('userEmail')!;          }
  get organizationName()  { return this.form.get('organizationName')!;  }
  get tin()               { return this.form.get('tin')!;               }
  get phone()             { return this.form.get('phone')!;             }
  get businessType()      { return this.form.get('businessType')!;      }
  get contactPersonName() { return this.form.get('contactPersonName')!; }
  get contactEmail()      { return this.form.get('contactEmail')!;      }

  onSave(): void {
    if (this.form.invalid) {
      this.form.markAllAsTouched();
      return;
    }

    this.loading = true;
    const v = this.form.getRawValue();

    if (this.isEdit) {
      const dto = {
        organizationName:  v.organizationName.trim(),
        tin:               v.tin.trim(),
        phone:             v.phone.trim(),
        businessType:      v.businessType,
        contactPersonName: v.contactPersonName.trim(),
        contactEmail:      v.contactEmail.trim().toLowerCase(),
        isActive:          v.isActive,
      };

      this.tenantService.update(this.data.tenant!.id, dto)
        .pipe(finalize(() => (this.loading = false)))
        .subscribe({
          next: tenant => {
            this.snackBar.open('Tenant updated successfully.', undefined, {
              duration: 3500, panelClass: ['snack-success'],
            });
            this.dialogRef.close(tenant);
          },
          error: err => {
            const msg = err.error?.message ?? 'Failed to update tenant.';
            this.snackBar.open(msg, 'Dismiss', { duration: 5000, panelClass: ['snack-error'] });
          },
        });
    } else {
      const dto = {
        userEmail:         v.userEmail.trim().toLowerCase(),
        organizationName:  v.organizationName.trim(),
        tin:               v.tin.trim(),
        phone:             v.phone.trim(),
        businessType:      v.businessType,
        contactPersonName: v.contactPersonName.trim(),
        contactEmail:      v.contactEmail.trim().toLowerCase(),
      };

      this.tenantService.create(dto)
        .pipe(finalize(() => (this.loading = false)))
        .subscribe({
          next: tenant => {
            this.snackBar.open('Tenant registered successfully.', undefined, {
              duration: 3500, panelClass: ['snack-success'],
            });
            this.dialogRef.close(tenant);
          },
          error: err => {
            const msg = err.error?.message ?? 'Failed to register tenant.';
            this.snackBar.open(msg, 'Dismiss', { duration: 5000, panelClass: ['snack-error'] });
          },
        });
    }
  }

  onCancel(): void {
    this.dialogRef.close(null);
  }
}
