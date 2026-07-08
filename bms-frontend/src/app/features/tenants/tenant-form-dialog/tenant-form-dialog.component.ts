import { Component, Inject, OnInit, OnDestroy } from '@angular/core';
import { CommonModule } from '@angular/common';
import { ReactiveFormsModule, FormsModule, FormBuilder, FormGroup, Validators } from '@angular/forms';
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
import { MatTooltipModule } from '@angular/material/tooltip';
import { Subject, of } from 'rxjs';
import { finalize, switchMap, takeUntil, catchError } from 'rxjs/operators';

import { TenantService } from '../tenant.service';
import { UnitService } from '../../units/unit.service';
import { TenantDto, RegisteredUserDto, BUSINESS_TYPES } from '../../../shared/models/tenant.models';
import { UnitDto } from '../../../shared/models/unit.models';

export interface TenantFormDialogData {
  tenant?: TenantDto;
}

@Component({
  selector: 'app-tenant-form-dialog',
  standalone: true,
  imports: [
    CommonModule,
    ReactiveFormsModule,
    FormsModule,
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
    MatTooltipModule,
  ],
  templateUrl: './tenant-form-dialog.component.html',
  styleUrls: ['./tenant-form-dialog.component.scss'],
})
export class TenantFormDialogComponent implements OnInit, OnDestroy {

  form!: FormGroup;
  loading = false;
  isEdit  = false;

  readonly businessTypes = BUSINESS_TYPES;

  /** Full list of registered users loaded from the backend. */
  registeredUsers: RegisteredUserDto[] = [];
  filteredUsers:   RegisteredUserDto[] = [];

  /** The resolved user ID after a pick — this is what gets submitted. */
  selectedUserId: string | null = null;

  // ── Unit reservation (create mode only) ──────────────────────────────────

  /** Available units for the reservation picker. Loaded once in create mode. */
  availableUnits: UnitDto[] = [];
  loadingUnits    = false;

  /**
   * Unit chosen for reservation — null means "no reservation".
   * This is a separate local variable (not a form control) so the
   * mat-select can be fully optional without polluting form validation.
   */
  selectedUnitId: number | null = null;

  private destroy$ = new Subject<void>();

  constructor(
    private fb:            FormBuilder,
    private tenantService: TenantService,
    private unitService:   UnitService,
    private snackBar:      MatSnackBar,
    public  dialogRef:     MatDialogRef<TenantFormDialogComponent>,
    @Inject(MAT_DIALOG_DATA) public data: TenantFormDialogData,
  ) {}

  ngOnInit(): void {
    this.isEdit = !!this.data?.tenant;
    const t = this.data?.tenant;

    this.form = this.fb.group({
      // Display-only autocomplete field — the actual ID is in selectedUserId.
      // Not required: Admin can create an unlinked tenant.
      userSearch:        [''],
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

    // Create-mode only: load registered users + available units
    if (!this.isEdit) {
      // Load user list for the owner picker
      this.tenantService.getRegisteredUsers()
        .pipe(takeUntil(this.destroy$))
        .subscribe({
          next: users => {
            this.registeredUsers = users;
            this.filteredUsers   = users;
          },
        });

      // Re-filter the user dropdown as the user types
      this.form.get('userSearch')!.valueChanges
        .pipe(takeUntil(this.destroy$))
        .subscribe(value => {
          // If the value doesn't match a picked user, clear the resolved ID
          if (this.selectedUserId) {
            const picked = this.registeredUsers.find(u => u.id === this.selectedUserId);
            if (picked && this.displayUser(picked) !== value) {
              this.selectedUserId = null;
            }
          }
          const term = (value ?? '').toLowerCase();
          this.filteredUsers = this.registeredUsers.filter(
            u => u.email.toLowerCase().includes(term) || u.fullName.toLowerCase().includes(term)
          );
        });

      // Load available units for the reservation picker
      this.loadAvailableUnits();
    }
  }

  ngOnDestroy(): void {
    this.destroy$.next();
    this.destroy$.complete();
  }

  // ── Unit reservation helpers ─────────────────────────────────────────────

  private loadAvailableUnits(): void {
    this.loadingUnits = true;
    this.unitService.getAll()
      .pipe(
        finalize(() => (this.loadingUnits = false)),
        takeUntil(this.destroy$),
      )
      .subscribe({
        next: units => {
          // Only show units that are Available — Reserved/Occupied/UnderMaintenance
          // cannot be reserved again.
          this.availableUnits = units.filter(u => u.status === 'Available');
        },
        error: () => {
          // Non-critical — just leave the picker empty; the admin can still
          // create the tenant without reserving a unit.
          this.availableUnits = [];
        },
      });
  }

  // ── User picker helpers ──────────────────────────────────────────────────

  /** Called when a user is selected from the autocomplete dropdown. */
  onUserSelected(user: RegisteredUserDto): void {
    this.selectedUserId = user.id;
    this.form.get('userSearch')!.setValue(this.displayUser(user), { emitEvent: false });
    this.filteredUsers = this.registeredUsers;
  }

  /** Clear the user selection. */
  clearUserSelection(): void {
    this.selectedUserId = null;
    this.form.get('userSearch')!.setValue('');
    this.filteredUsers = this.registeredUsers;
  }

  /** Display function for the autocomplete input. */
  displayUser(user: RegisteredUserDto | null): string {
    if (!user) return '';
    return `${user.fullName} (${user.email})`;
  }

  // ── Convenience getters ──────────────────────────────────────────────────
  get organizationName()  { return this.form.get('organizationName')!;  }
  get tin()               { return this.form.get('tin')!;               }
  get phone()             { return this.form.get('phone')!;             }
  get businessType()      { return this.form.get('businessType')!;      }
  get contactPersonName() { return this.form.get('contactPersonName')!; }
  get contactEmail()      { return this.form.get('contactEmail')!;      }

  // ── Submit ───────────────────────────────────────────────────────────────

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
          error: err => this.showError(err, 'Failed to update tenant.'),
        });

    } else {
      const dto = {
        appUserId:         this.selectedUserId ?? undefined,
        organizationName:  v.organizationName.trim(),
        tin:               v.tin.trim(),
        phone:             v.phone.trim(),
        businessType:      v.businessType,
        contactPersonName: v.contactPersonName.trim(),
        contactEmail:      v.contactEmail.trim().toLowerCase(),
      };

      // Create the tenant, then (if a unit was picked) immediately reserve it.
      this.tenantService.create(dto)
        .pipe(
          switchMap(tenant => {
            if (this.selectedUnitId !== null) {
              // Reserve the chosen unit for the newly created tenant.
              // If reservation fails we still close with the tenant — the admin
              // can reserve manually. A warning snack is shown instead of blocking.
              return this.unitService.reserve(this.selectedUnitId, tenant.id).pipe(
                switchMap(() => of(tenant)),
                catchError(reserveErr => {
                  const msg = reserveErr?.error?.message ?? 'Unit reservation failed — tenant was created but unit was not reserved.';
                  this.snackBar.open(msg, 'Dismiss', { duration: 6000, panelClass: ['snack-warning'] });
                  return of(tenant);   // still close with the created tenant
                }),
              );
            }
            return of(tenant);
          }),
          finalize(() => (this.loading = false)),
          takeUntil(this.destroy$),
        )
        .subscribe({
          next: tenant => {
            const unitMsg = this.selectedUnitId !== null
              ? ' Unit reserved successfully.'
              : '';
            this.snackBar.open(`Tenant registered successfully.${unitMsg}`, undefined, {
              duration: 3500, panelClass: ['snack-success'],
            });
            this.dialogRef.close(tenant);
          },
          error: err => {
            // Distinguish tenant-creation errors from reservation errors
            const msg = err?.error?.message ?? 'Failed to register tenant.';
            this.snackBar.open(msg, 'Dismiss', { duration: 6000, panelClass: ['snack-error'] });
          },
        });
    }
  }

  onCancel(): void {
    this.dialogRef.close(null);
  }

  private showError(err: any, fallback: string): void {
    const msg = err?.error?.message ?? fallback;
    this.snackBar.open(msg, 'Dismiss', { duration: 6000, panelClass: ['snack-error'] });
  }
}
