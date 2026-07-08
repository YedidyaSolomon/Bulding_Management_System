import { Component, Inject, OnInit } from '@angular/core';
import { CommonModule } from '@angular/common';
import {
  ReactiveFormsModule, FormBuilder, FormGroup,
  Validators, AbstractControl, ValidationErrors,
} from '@angular/forms';
import { MatDialogRef, MAT_DIALOG_DATA, MatDialogModule, MatDialog } from '@angular/material/dialog';
import { MatButtonModule } from '@angular/material/button';
import { MatIconModule } from '@angular/material/icon';
import { MatDividerModule } from '@angular/material/divider';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatInputModule } from '@angular/material/input';
import { MatSelectModule } from '@angular/material/select';
import { MatProgressSpinnerModule } from '@angular/material/progress-spinner';
import { MatSnackBar, MatSnackBarModule } from '@angular/material/snack-bar';
import { MatTooltipModule } from '@angular/material/tooltip';
import { finalize } from 'rxjs/operators';

import { TenantService } from '../tenant.service';
import { AuthService } from '../../../core/services/auth.service';
import {
  TenantDto,
  LegalDocumentDto,
  DOCUMENT_TYPES,
  DOCUMENT_TYPE_LABELS,
} from '../../../shared/models/tenant.models';
import {
  AppDatePickerComponent,
  dateToIso,
} from '../../../shared/components/date-picker/date-picker.component';
import {
  TenantLinkUserDialogComponent,
  TenantLinkUserDialogData,
} from '../tenant-link-user-dialog/tenant-link-user-dialog.component';

export interface TenantDetailDialogData { tenant: TenantDto; }

// ── Validator ─────────────────────────────────────────────────────────────────
function notInPastValidator(ctrl: AbstractControl): ValidationErrors | null {
  if (!ctrl.value) return null;
  const sel = new Date(ctrl.value); sel.setHours(0, 0, 0, 0);
  const now = new Date();           now.setHours(0, 0, 0, 0);
  return sel < now ? { pastDate: true } : null;
}

// ─────────────────────────────────────────────────────────────────────────────

@Component({
  selector: 'app-tenant-detail-dialog',
  standalone: true,
  imports: [
    CommonModule,
    ReactiveFormsModule,
    MatDialogModule,
    MatButtonModule,
    MatIconModule,
    MatDividerModule,
    MatFormFieldModule,
    MatInputModule,
    MatSelectModule,
    MatProgressSpinnerModule,
    MatSnackBarModule,
    MatTooltipModule,
    AppDatePickerComponent,
  ],
  templateUrl: './tenant-detail-dialog.component.html',
  styleUrls: ['./tenant-detail-dialog.component.scss'],
})
export class TenantDetailDialogComponent implements OnInit {

  /** Local copy — updated when the link-user dialog closes with a result. */
  tenant!: TenantDto;

  documents:  LegalDocumentDto[] = [];
  loadingDocs = false;
  showDocForm = false;
  savingDoc   = false;
  docForm!:   FormGroup;

  readonly minExpiryDate  = new Date();
  readonly documentTypes      = DOCUMENT_TYPES;
  readonly documentTypeLabels = DOCUMENT_TYPE_LABELS;

  constructor(
    private tenantService: TenantService,
    private authService:   AuthService,
    private snackBar:      MatSnackBar,
    private fb:            FormBuilder,
    private dialog:        MatDialog,
    public  dialogRef:     MatDialogRef<TenantDetailDialogComponent>,
    @Inject(MAT_DIALOG_DATA) public data: TenantDetailDialogData,
  ) {}

  ngOnInit(): void {
    // Work from a local copy so we can update it without mutating the parent list
    this.tenant = { ...this.data.tenant };
    this.loadDocuments();

    this.docForm = this.fb.group({
      documentType: ['',   Validators.required],
      filePath:     ['',   [Validators.required, Validators.maxLength(500)]],
      expiryDate:   [null, notInPastValidator],
    });
  }

  // ── Role helpers ──────────────────────────────────────────────────────────
  get canManage(): boolean {
    const role = this.authService.getRole();
    return role === 'Admin' || role === 'Manager';
  }

  // ── Ownership section ─────────────────────────────────────────────────────

  openLinkUser(): void {
    const dialogData: TenantLinkUserDialogData = { tenant: this.tenant };

    this.dialog
      .open(TenantLinkUserDialogComponent, {
        width: '520px', maxWidth: '95vw', data: dialogData,
      })
      .afterClosed()
      .subscribe((updated: TenantDto | null) => {
        if (updated) {
          // Update local copy so the ownership section refreshes immediately
          this.tenant = updated;
        }
      });
  }

  // ── Documents ─────────────────────────────────────────────────────────────

  loadDocuments(): void {
    this.loadingDocs = true;
    this.tenantService.getDocuments(this.tenant.id)
      .subscribe({
        next: docs  => { this.documents   = docs;  this.loadingDocs = false; },
        error: ()   => { this.loadingDocs = false; },
      });
  }

  toggleDocForm(): void {
    this.showDocForm = !this.showDocForm;
    if (!this.showDocForm) this.docForm.reset();
  }

  onAddDocument(): void {
    if (this.docForm.invalid) {
      this.docForm.markAllAsTouched();
      return;
    }

    this.savingDoc = true;
    const v = this.docForm.value;

    const expiryIso: string | null = v.expiryDate ? dateToIso(v.expiryDate) : null;

    this.tenantService.addDocument(this.tenant.id, {
      tenantId:     this.tenant.id,
      documentType: v.documentType,
      filePath:     v.filePath.trim(),
      expiryDate:   expiryIso,
    }).pipe(finalize(() => (this.savingDoc = false)))
      .subscribe({
        next: doc => {
          this.documents   = [...this.documents, doc];
          this.showDocForm = false;
          this.docForm.reset();
          this.snackBar.open('Document added successfully.', undefined, {
            duration: 3000, panelClass: ['snack-success'],
          });
        },
        error: err => {
          const msg = err.error?.message ?? 'Failed to add document.';
          this.snackBar.open(msg, 'Dismiss', { duration: 5000, panelClass: ['snack-error'] });
        },
      });
  }

  docTypeLabel(type: string): string { return this.documentTypeLabels[type] ?? type; }
  statusClass(isActive: boolean):  string { return isActive ? 'badge-success' : 'badge-warning'; }
  statusLabel(isActive: boolean):  string { return isActive ? 'Active' : 'Inactive'; }

  close(): void { this.dialogRef.close(); }
}
