import { Component, OnInit, OnDestroy, ViewChild, NgZone } from '@angular/core';
import { CommonModule } from '@angular/common';
import { ReactiveFormsModule, FormControl } from '@angular/forms';
import { MatTableModule, MatTableDataSource } from '@angular/material/table';
import { MatPaginator, MatPaginatorModule, PageEvent } from '@angular/material/paginator';
import { MatSort, MatSortModule, Sort } from '@angular/material/sort';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatInputModule } from '@angular/material/input';
import { MatSelectModule } from '@angular/material/select';
import { MatButtonModule } from '@angular/material/button';
import { MatIconModule } from '@angular/material/icon';
import { MatTooltipModule } from '@angular/material/tooltip';
import { MatDialog, MatDialogModule } from '@angular/material/dialog';
import { MatSnackBarModule } from '@angular/material/snack-bar';
import { MatProgressSpinnerModule } from '@angular/material/progress-spinner';
import { Subject } from 'rxjs';
import { debounceTime, distinctUntilChanged, takeUntil } from 'rxjs/operators';

import { AuthService } from '../../core/services/auth.service';
import { PaymentService } from './payment.service';
import { InvoiceService } from '../invoices/invoice.service';
import {
  PaymentDto,
  PAYMENT_METHODS,
  PAYMENT_METHOD_LABELS,
  PAYMENT_METHOD_ICONS,
} from '../../shared/models/payment.models';
import { PaymentRecordDialogComponent } from './payment-record-dialog/payment-record-dialog.component';
import { PaymentDetailDialogComponent } from './payment-detail-dialog/payment-detail-dialog.component';
import { EtbCurrencyPipe } from '../../shared/pipes/etb-currency.pipe';

@Component({
  selector: 'app-payments',
  standalone: true,
  imports: [
    CommonModule,
    ReactiveFormsModule,
    MatTableModule,
    MatPaginatorModule,
    MatSortModule,
    MatFormFieldModule,
    MatInputModule,
    MatSelectModule,
    MatButtonModule,
    MatIconModule,
    MatTooltipModule,
    MatDialogModule,
    MatSnackBarModule,
    MatProgressSpinnerModule,
    EtbCurrencyPipe,
  ],
  templateUrl: './payments.component.html',
  styleUrls:  ['./payments.component.scss'],
})
export class PaymentsComponent implements OnInit, OnDestroy {

  @ViewChild(MatPaginator) paginator!: MatPaginator;
  @ViewChild(MatSort)      matSort!:   MatSort;

  displayedColumns = [
    'paymentDate', 'invoiceNumber', 'paymentMethod',
    'amountPaid', 'referenceNumber', 'actions',
  ];

  dataSource  = new MatTableDataSource<PaymentDto>([]);
  allPayments: PaymentDto[] = [];
  loading     = false;

  // Filters
  searchControl = new FormControl('');
  methodFilter  = new FormControl('all');

  readonly paymentMethods      = PAYMENT_METHODS;
  readonly paymentMethodLabels = PAYMENT_METHOD_LABELS;
  readonly paymentMethodIcons  = PAYMENT_METHOD_ICONS;

  // Skeleton placeholder rows (shown while loading)
  readonly skeletonRows = Array(6).fill(0);

  // Pagination
  pageSize        = 10;
  pageIndex       = 0;
  totalCount      = 0;
  pageSizeOptions = [5, 10, 25, 50];

  // ── Count-up animation state ──────────────────────────────────────────────
  // Displayed values animate from 0 → final over ~650 ms.
  animTotalAmount  = 0;
  animTodayAmount  = 0;
  animCashCount    = 0;
  animTransferCount = 0;
  animTotalCount   = 0;

  private destroy$ = new Subject<void>();

  constructor(
    private paymentService: PaymentService,
    private invoiceService: InvoiceService,
    private authService:    AuthService,
    private dialog:         MatDialog,
    private ngZone:         NgZone,
  ) {}

  get role():      string  { return this.authService.getRole() ?? ''; }
  get canRecord(): boolean { return this.role === 'Admin' || this.role === 'Manager'; }

  // ── Stats (raw values used as targets for count-up) ───────────────────────
  get totalAmount():    number { return this.allPayments.reduce((s, p) => s + p.amountPaid, 0); }
  get todayAmount():    number {
    const today = new Date().toISOString().substring(0, 10);
    return this.allPayments
      .filter(p => p.paymentDate.substring(0, 10) === today)
      .reduce((s, p) => s + p.amountPaid, 0);
  }
  get cashCount():     number { return this.allPayments.filter(p => p.paymentMethod === 'Cash').length; }
  get transferCount(): number { return this.allPayments.filter(p => p.paymentMethod === 'BankTransfer').length; }

  // ── Count-up animation ────────────────────────────────────────────────────
  private animateCountUp(
    targetRaw: number,
    setter: (v: number) => void,
    isFloat = false,
    durationMs = 650,
  ): void {
    const steps   = 40;
    const stepMs  = durationMs / steps;
    let   current = 0;

    // Run outside Angular zone so we don't trigger excessive CD ticks;
    // step into the zone only for the final value to trigger a clean render.
    this.ngZone.runOutsideAngular(() => {
      const interval = setInterval(() => {
        current++;
        const progress = current / steps;
        // Ease-out: fast start, decelerates at the end
        const eased    = 1 - Math.pow(1 - progress, 3);
        const value    = isFloat
          ? Math.round(targetRaw * eased * 100) / 100
          : Math.round(targetRaw * eased);

        if (current >= steps) {
          clearInterval(interval);
          this.ngZone.run(() => setter(targetRaw));
        } else {
          this.ngZone.run(() => setter(value));
        }
      }, stepMs);
    });
  }

  private runAllCountUps(): void {
    this.animateCountUp(this.totalAmount,    v => this.animTotalAmount   = v, true);
    this.animateCountUp(this.todayAmount,    v => this.animTodayAmount   = v, true);
    this.animateCountUp(this.cashCount,      v => this.animCashCount     = v);
    this.animateCountUp(this.transferCount,  v => this.animTransferCount = v);
    this.animateCountUp(this.allPayments.length, v => this.animTotalCount = v);
  }

  // ── Method color class ────────────────────────────────────────────────────
  /** Returns a BEM modifier class for the payment method, e.g. "method--cash" */
  methodColorClass(method: string): string {
    const map: Record<string, string> = {
      Cash:        'method--cash',
      BankTransfer:'method--transfer',
      Cheque:      'method--cheque',
      MobileMoney: 'method--mobile',
    };
    return map[method] ?? 'method--default';
  }

  ngOnInit(): void {
    this.loadPayments();

    this.searchControl.valueChanges
      .pipe(debounceTime(300), distinctUntilChanged(), takeUntil(this.destroy$))
      .subscribe(() => { this.pageIndex = 0; this.applyFilters(); });

    this.methodFilter.valueChanges
      .pipe(takeUntil(this.destroy$))
      .subscribe(() => { this.pageIndex = 0; this.applyFilters(); });
  }

  ngOnDestroy(): void {
    this.destroy$.next();
    this.destroy$.complete();
  }

  loadPayments(): void {
    this.loading = true;
    const request$ = this.role === 'Viewer'
      ? this.paymentService.getMyPayments()
      : this.paymentService.getAll();

    request$
      .pipe(takeUntil(this.destroy$))
      .subscribe({
        next: payments => {
          this.allPayments = payments;
          this.applyFilters();
          this.loading = false;
          // Kick off count-up after a single frame so the DOM is painted
          requestAnimationFrame(() => this.runAllCountUps());
        },
        error: () => { this.loading = false; },
      });
  }

  applyFilters(): void {
    const search = (this.searchControl.value ?? '').toLowerCase().trim();
    const method = this.methodFilter.value ?? 'all';

    let filtered = this.allPayments.filter(p => {
      const matchSearch =
        !search ||
        p.invoiceNumber.toLowerCase().includes(search)   ||
        (p.referenceNumber ?? '').toLowerCase().includes(search);
      const matchMethod = method === 'all' || p.paymentMethod === method;
      return matchSearch && matchMethod;
    });

    this.totalCount = filtered.length;

    const active = this.matSort?.active;
    const dir    = this.matSort?.direction;
    if (active && dir) filtered = this.sortData(filtered, active, dir);

    const start = this.pageIndex * this.pageSize;
    this.dataSource.data = filtered.slice(start, start + this.pageSize);
  }

  onSortChange(_sort: Sort): void { this.pageIndex = 0; this.applyFilters(); }

  onPageChange(event: PageEvent): void {
    this.pageIndex = event.pageIndex;
    this.pageSize  = event.pageSize;
    this.applyFilters();
  }

  clearFilters(): void {
    this.searchControl.setValue('');
    this.methodFilter.setValue('all');
    this.pageIndex = 0;
  }

  get hasActiveFilters(): boolean {
    return !!this.searchControl.value || this.methodFilter.value !== 'all';
  }

  methodLabel(m: string): string { return this.paymentMethodLabels[m] ?? m; }
  methodIcon(m: string):  string { return this.paymentMethodIcons[m]  ?? 'payments'; }

  // ── Dialogs ────────────────────────────────────────────────────────────────
  openRecord(): void {
    this.invoiceService.getAll().subscribe({
      next: invoices => {
        const payable = invoices.filter(
          i => i.status === 'Issued' || i.status === 'Overdue'
        );
        if (payable.length === 0) return;

        this.dialog.open(PaymentRecordDialogComponent, {
          width: '520px', maxWidth: '95vw',
          data: { invoice: payable[0], invoices: payable },
        }).afterClosed().subscribe(result => {
          if (result) this.loadPayments();
        });
      },
      error: () => {},
    });
  }

  openDetail(payment: PaymentDto): void {
    this.dialog.open(PaymentDetailDialogComponent, {
      width: '480px', maxWidth: '95vw',
      data: { payment },
    });
  }

  // ── Sort helper ────────────────────────────────────────────────────────────
  private sortData(data: PaymentDto[], active: string, direction: string): PaymentDto[] {
    if (!direction) return data;
    return [...data].sort((a, b) => {
      let valA: string | number;
      let valB: string | number;
      switch (active) {
        case 'paymentDate':   valA = a.paymentDate;   valB = b.paymentDate;   break;
        case 'amountPaid':    valA = a.amountPaid;    valB = b.amountPaid;    break;
        case 'invoiceNumber': valA = a.invoiceNumber; valB = b.invoiceNumber; break;
        case 'paymentMethod': valA = a.paymentMethod; valB = b.paymentMethod; break;
        default: return 0;
      }
      const cmp = valA < valB ? -1 : valA > valB ? 1 : 0;
      return direction === 'asc' ? cmp : -cmp;
    });
  }
}
