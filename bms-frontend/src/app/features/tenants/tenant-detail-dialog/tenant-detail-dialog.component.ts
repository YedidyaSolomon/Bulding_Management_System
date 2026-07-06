import { Component, Inject, OnInit } from '@angular/core';
import { CommonModule } from '@angular/common';
import {
  ReactiveFormsModule, FormBuilder, FormGroup,
  Validators, AbstractControl, ValidationErrors,
} from '@angular/forms';
import { MatDialogRef, MAT_DIALOG_DATA, MatDialogModule } from '@angular/material/dialog';
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

export interface TenantDetailDialogData { tenant: TenantDto; }

// ── Validator ─────────────────────────────────────────────────────────────────

/**
 * Control-level: if a date is provided it must be today or in the future.
 * The field is optional so a null/empty value is always valid here.
 */
function notInPastValidator(ctrl: AbstractControl): ValidationErrors | null {
  if (!ctrl.value) return null;          // optional — null is fine
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

  documents:  LegalDocumentDto[] = [];
  loadingDocs = false;
  showDocForm = false;
  savingDoc   = false;
  docForm!:   FormGroup;

  /** Expiry date must be today or later (passed to picker [minDate]). */
  readonly minExpiryDate = new Date();

  readonly documentTypes      = DOCUMENT_TYPES;
  readonly documentTypeLabels = DOCUMENT_TYPE_LABELS;

  constructor(
    private tenantService: TenantService,
    private snackBar:      MatSnackBar,
    private fb:            FormBuilder,
    public  dialogRef:     MatDialogRef<TenantDetailDialogComponent>,
    @Inject(MAT_DIALOG_DATA) public data: TenantDetailDialogData,
  ) {}

  ngOnInit(): void {
    this.loadDocuments();

    this.docForm = this.fb.group({
      documentType: ['',   Validators.required],
      filePath:     ['',   [Validators.required, Validators.maxLength(500)]],
      // expiryDate is optional; when provided must be today or later
      expiryDate:   [null, notInPastValidator],
    });
  }

  loadDocuments(): void {
    this.loadingDocs = true;
    this.tenantService.getDocuments(this.data.tenant.id)
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

    // expiryDate is a Date | null from the picker — convert to ISO string | null
    const expiryIso: string | null = v.expiryDate
      ? dateToIso(v.expiryDate)
      : null;

    this.tenantService.addDocument(this.data.tenant.id, {
      tenantId:     this.data.tenant.id,
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

  statusClass(isActive: boolean): string { return isActive ? 'badge-success' : 'badge-warning'; }
  statusLabel(isActive: boolean): string { return isActive ? 'Active' : 'Inactive'; }

  close(): void { this.dialogRef.close(); }
}
