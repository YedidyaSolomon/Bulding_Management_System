import { Component, OnInit, OnDestroy } from '@angular/core';
import { CommonModule } from '@angular/common';
import { ReactiveFormsModule, FormControl } from '@angular/forms';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatInputModule } from '@angular/material/input';
import { MatSelectModule } from '@angular/material/select';
import { MatButtonModule } from '@angular/material/button';
import { MatIconModule } from '@angular/material/icon';
import { MatTooltipModule } from '@angular/material/tooltip';
import { MatDialog, MatDialogModule } from '@angular/material/dialog';
import { MatSnackBar, MatSnackBarModule } from '@angular/material/snack-bar';
import { MatProgressSpinnerModule } from '@angular/material/progress-spinner';
import { MatPaginatorModule, PageEvent } from '@angular/material/paginator';
import { Subject } from 'rxjs';
import { debounceTime, distinctUntilChanged, takeUntil, finalize } from 'rxjs/operators';

import { AuthService } from '../../core/services/auth.service';
import { InvoiceService } from './invoice.service';
import { InvoiceDto, INVOICE_STATUSES, INVOICE_STATUS_LABELS } from '../../shared/models/invoice.models';
import { InvoiceCardComponent } from './invoice-card/invoice-card.component';
import { InvoiceGenerateDialogComponent } from './invoice-generate-dialog/invoice-generate-dialog.component';
import { InvoiceDetailDialogComponent } from './invoice-detail-dialog/invoice-detail-dialog.component';
import { PaymentRecordDialogComponent } from '../payments/payment-record-dialog/payment-record-dialog.component';

@Component({
  selector: 'app-invoices',
  standalone: true,
  imports: [
    CommonModule,
    ReactiveFormsModule,
    MatFormFieldModule,
    MatInputModule,
    MatSelectModule,
    MatButtonModule,
    MatIconModule,
    MatTooltipModule,
    MatDialogModule,
    MatSnackBarModule,
    MatProgressSpinnerModule,
    MatPaginatorModule,
    InvoiceCardComponent,
  ],
  templateUrl: './invoices.component.html',
  styleUrls:  ['./invoices.component.scss'],
})
export class InvoicesComponent implements OnInit, OnDestroy {

  allInvoices:       InvoiceDto[] = [];
  displayedInvoices: InvoiceDto[] = [];
  loading = false;

  // Filters
  searchControl = new FormControl('');
  statusFilter  = new FormControl('all');
  sortControl   = new FormControl('issueDate-desc');

  readonly invoiceStatuses     = INVOICE_STATUSES;
  readonly invoiceStatusLabels = INVOICE_STATUS_LABELS;

  // Pagination
  pageSize        = 12;
  pageIndex       = 0;
  totalCount      = 0;
  pageSizeOptions = [6, 12, 24, 48];

  private destroy$ = new Subject<void>();

  constructor(
    private invoiceService: InvoiceService,
    private authService:    AuthService,
    private dialog:         MatDialog,
    private snackBar:       MatSnackBar,
  ) {}

  get role():       string  { return this.authService.getRole() ?? ''; }
  get canManage():  boolean { return this.role === 'Admin' || this.role === 'Manager'; }
  get canCancel():  boolean { return this.role === 'Admin'; }

  // ── Stats ──────────────────────────────────────────────────────────────────
  get draftCount():   number { return this.allInvoices.filter(i => i.status === 'Draft').length;     }
  get issuedCount():  number { return this.allInvoices.filter(i => i.status === 'Issued').length;    }
  get paidCount():    number { return this.allInvoices.filter(i => i.status === 'Paid').length;      }
  get overdueCount(): number { return this.allInvoices.filter(i => i.status === 'Overdue').length;   }

  get totalOutstanding(): number {
    return this.allInvoices
      .filter(i => i.status === 'Issued' || i.status === 'Overdue')
      .reduce((sum, i) => sum + i.amountDue, 0);
  }

  ngOnInit(): void {
    this.loadInvoices();

    this.searchControl.valueChanges
      .pipe(debounceTime(300), distinctUntilChanged(), takeUntil(this.destroy$))
      .subscribe(() => { this.pageIndex = 0; this.applyFilters(); });

    this.statusFilter.valueChanges
      .pipe(takeUntil(this.destroy$))
      .subscribe(() => { this.pageIndex = 0; this.applyFilters(); });

    this.sortControl.valueChanges
      .pipe(takeUntil(this.destroy$))
      .subscribe(() => { this.pageIndex = 0; this.applyFilters(); });
  }

  ngOnDestroy(): void {
    this.destroy$.next();
    this.destroy$.complete();
  }

  loadInvoices(): void {
    this.loading = true;
    // Viewer sees only their own invoices; Admin/Manager see all
    const request$ = this.role === 'Viewer'
      ? this.invoiceService.getMyInvoices()
      : this.invoiceService.getAll();

    request$
      .pipe(takeUntil(this.destroy$))
      .subscribe({
        next: invoices => {
          this.allInvoices = invoices;
          this.applyFilters();
          this.loading = false;
        },
        error: () => { this.loading = false; },
      });
  }

  applyFilters(): void {
    const search = (this.searchControl.value ?? '').toLowerCase().trim();
    const status = this.statusFilter.value ?? 'all';

    let filtered = this.allInvoices.filter(i => {
      const matchSearch =
        !search ||
        i.invoiceNumber.toLowerCase().includes(search) ||
        i.tenantName.toLowerCase().includes(search)    ||
        i.unitNumber.toLowerCase().includes(search);

      const matchStatus = status === 'all' || i.status === status;
      return matchSearch && matchStatus;
    });

    this.totalCount = filtered.length;

    // Sort
    const [field, dir] = (this.sortControl.value ?? 'issueDate-desc').split('-');
    filtered = this.sortData(filtered, field, dir);

    // Paginate
    const start = this.pageIndex * this.pageSize;
    this.displayedInvoices = filtered.slice(start, start + this.pageSize);
  }

  onPageChange(event: PageEvent): void {
    this.pageIndex = event.pageIndex;
    this.pageSize  = event.pageSize;
    this.applyFilters();
  }

  clearFilters(): void {
    this.searchControl.setValue('');
    this.statusFilter.setValue('all');
    this.pageIndex = 0;
  }

  get hasActiveFilters(): boolean {
    return !!this.searchControl.value || this.statusFilter.value !== 'all';
  }

  statusLabel(s: string): string { return this.invoiceStatusLabels[s] ?? s; }

  // ── Actions ────────────────────────────────────────────────────────────────

  openGenerate(): void {
    this.dialog.open(InvoiceGenerateDialogComponent, {
      width: '560px', maxWidth: '95vw',
    }).afterClosed().subscribe(result => {
      if (result) this.loadInvoices();
    });
  }

  openDetail(invoice: InvoiceDto): void {
    this.dialog.open(InvoiceDetailDialogComponent, {
      width: '600px', maxWidth: '95vw',
      data: { invoice, canManage: this.canManage, canCancel: this.canCancel },
    }).afterClosed().subscribe(result => {
      if (result) this.loadInvoices();
    });
  }

  onIssue(invoice: InvoiceDto): void {
    this.invoiceService.issue(invoice.id)
      .subscribe({
        next: () => {
          this.snackBar.open(`Invoice ${invoice.invoiceNumber} issued.`, undefined, {
            duration: 3000, panelClass: ['snack-success'],
          });
          this.loadInvoices();
        },
        error: err => {
          this.snackBar.open(err.error?.message ?? 'Failed to issue invoice.', 'Dismiss', {
            duration: 5000, panelClass: ['snack-error'],
          });
        },
      });
  }

  onCancel(invoice: InvoiceDto): void {
    this.invoiceService.cancel(invoice.id)
      .subscribe({
        next: () => {
          this.snackBar.open(`Invoice ${invoice.invoiceNumber} cancelled.`, undefined, {
            duration: 3000, panelClass: ['snack-success'],
          });
          this.loadInvoices();
        },
        error: err => {
          this.snackBar.open(err.error?.message ?? 'Failed to cancel invoice.', 'Dismiss', {
            duration: 5000, panelClass: ['snack-error'],
          });
        },
      });
  }

  openRecordPayment(invoice: InvoiceDto): void {
    this.dialog.open(PaymentRecordDialogComponent, {
      width: '520px', maxWidth: '95vw',
      data: { invoice },
    }).afterClosed().subscribe(result => {
      if (result) this.loadInvoices();
    });
  }

  // ── Sort ───────────────────────────────────────────────────────────────────

  private sortData(data: InvoiceDto[], field: string, dir: string): InvoiceDto[] {
    if (!field || !dir) return data;
    return [...data].sort((a, b) => {
      let valA: string | number;
      let valB: string | number;
      switch (field) {
        case 'issueDate': valA = a.issueDate;    valB = b.issueDate;    break;
        case 'dueDate':   valA = a.dueDate;      valB = b.dueDate;      break;
        case 'amount':    valA = a.amountDue;    valB = b.amountDue;    break;
        case 'tenant':    valA = a.tenantName;   valB = b.tenantName;   break;
        default:          return 0;
      }
      const cmp = valA < valB ? -1 : valA > valB ? 1 : 0;
      return dir === 'asc' ? cmp : -cmp;
    });
  }
}
