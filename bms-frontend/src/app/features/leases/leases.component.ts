import { Component, OnInit, OnDestroy, ViewChild } from '@angular/core';
import { CommonModule } from '@angular/common';
import { ReactiveFormsModule, FormControl } from '@angular/forms';
import { MatPaginator, MatPaginatorModule, PageEvent } from '@angular/material/paginator';
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
import { LeaseService } from './lease.service';
import { LeaseDto, LEASE_STATUSES, LEASE_STATUS_LABELS } from '../../shared/models/lease.models';
import { LeaseCardComponent } from './lease-card/lease-card.component';
import { LeaseFormDialogComponent } from './lease-form-dialog/lease-form-dialog.component';
import { LeaseTerminateDialogComponent } from './lease-terminate-dialog/lease-terminate-dialog.component';
import { LeaseDetailDialogComponent } from './lease-detail-dialog/lease-detail-dialog.component';

@Component({
  selector: 'app-leases',
  standalone: true,
  imports: [
    CommonModule,
    ReactiveFormsModule,
    MatPaginatorModule,
    MatFormFieldModule,
    MatInputModule,
    MatSelectModule,
    MatButtonModule,
    MatIconModule,
    MatTooltipModule,
    MatDialogModule,
    MatSnackBarModule,
    MatProgressSpinnerModule,
    LeaseCardComponent,
  ],
  templateUrl: './leases.component.html',
  styleUrls:  ['./leases.component.scss'],
})
export class LeasesComponent implements OnInit, OnDestroy {

  @ViewChild(MatPaginator) paginator!: MatPaginator;

  allLeases:       LeaseDto[] = [];
  displayedLeases: LeaseDto[] = [];
  loading = false;

  // Filters
  searchControl  = new FormControl('');
  statusFilter   = new FormControl('all');
  sortControl    = new FormControl('startDate-desc');

  readonly leaseStatuses     = LEASE_STATUSES;
  readonly leaseStatusLabels = LEASE_STATUS_LABELS;

  // Pagination
  pageSize        = 12;
  pageIndex       = 0;
  totalCount      = 0;
  pageSizeOptions = [6, 12, 24, 48];

  private destroy$ = new Subject<void>();

  constructor(
    private leaseService: LeaseService,
    private authService:  AuthService,
    private dialog:       MatDialog,
  ) {}

  get role():         string  { return this.authService.getRole() ?? ''; }
  get canCreate():    boolean { return this.role === 'Admin' || this.role === 'Manager'; }
  get canEdit():      boolean { return this.role === 'Admin' || this.role === 'Manager'; }
  get canTerminate(): boolean { return this.role === 'Admin' || this.role === 'Manager'; }

  // ── Stats ──────────────────────────────────────────────────────────────────
  get activeCount():        number { return this.allLeases.filter(l => l.status === 'Active').length; }
  get expiredCount():       number { return this.allLeases.filter(l => l.status === 'Expired').length; }
  get terminatedCount():    number { return this.allLeases.filter(l => l.status === 'Terminated').length; }
  get pendingRenewalCount():number { return this.allLeases.filter(l => l.status === 'PendingRenewal').length; }
  get expiringIn30():       number {
    const cutoff = new Date();
    cutoff.setDate(cutoff.getDate() + 30);
    return this.allLeases.filter(l => {
      if (l.status !== 'Active') return false;
      const end = new Date(l.endDate);
      return end <= cutoff && end >= new Date();
    }).length;
  }

  ngOnInit(): void {
    this.loadLeases();

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

  loadLeases(): void {
    this.loading = true;
    this.leaseService.getAll()
      .pipe(takeUntil(this.destroy$))
      .subscribe({
        next: leases => {
          this.allLeases = leases;
          this.applyFilters();
          this.loading = false;
        },
        error: () => { this.loading = false; },
      });
  }

  applyFilters(): void {
    const search = (this.searchControl.value ?? '').toLowerCase().trim();
    const status = this.statusFilter.value ?? 'all';

    let filtered = this.allLeases.filter(l => {
      const matchSearch =
        !search ||
        l.unitNumber.toLowerCase().includes(search)  ||
        l.tenantName.toLowerCase().includes(search);

      const matchStatus = status === 'all' || l.status === status;

      return matchSearch && matchStatus;
    });

    this.totalCount = filtered.length;

    // Sort
    const [field, dir] = (this.sortControl.value ?? 'startDate-desc').split('-');
    filtered = this.sortData(filtered, field, dir);

    // Paginate
    const start = this.pageIndex * this.pageSize;
    this.displayedLeases = filtered.slice(start, start + this.pageSize);
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

  statusLabel(s: string): string { return this.leaseStatusLabels[s] ?? s; }

  // ── Dialogs ────────────────────────────────────────────────────────────────

  openCreate(): void {
    this.dialog.open(LeaseFormDialogComponent, {
      width: '640px', maxWidth: '95vw', data: {},
    }).afterClosed().subscribe(result => {
      if (result) this.loadLeases();
    });
  }

  openEdit(lease: LeaseDto): void {
    this.dialog.open(LeaseFormDialogComponent, {
      width: '640px', maxWidth: '95vw', data: { lease },
    }).afterClosed().subscribe(result => {
      if (result) this.loadLeases();
    });
  }

  openTerminate(lease: LeaseDto): void {
    this.dialog.open(LeaseTerminateDialogComponent, {
      width: '480px', maxWidth: '95vw', data: { lease },
    }).afterClosed().subscribe(done => {
      if (done) this.loadLeases();
    });
  }

  openDetail(lease: LeaseDto): void {
    this.dialog.open(LeaseDetailDialogComponent, {
      width: '560px', maxWidth: '95vw', data: { lease },
    });
  }

  // ── Sort helper ────────────────────────────────────────────────────────────

  private sortData(data: LeaseDto[], field: string, dir: string): LeaseDto[] {
    if (!field || !dir) return data;
    return [...data].sort((a, b) => {
      let valA: string | number;
      let valB: string | number;
      switch (field) {
        case 'startDate':   valA = a.startDate;    valB = b.startDate;    break;
        case 'endDate':     valA = a.endDate;      valB = b.endDate;      break;
        case 'monthlyRent': valA = a.monthlyRent;  valB = b.monthlyRent;  break;
        case 'tenant':      valA = a.tenantName;   valB = b.tenantName;   break;
        case 'unit':        valA = a.unitNumber;   valB = b.unitNumber;   break;
        default:            return 0;
      }
      const cmp = valA < valB ? -1 : valA > valB ? 1 : 0;
      return dir === 'asc' ? cmp : -cmp;
    });
  }
}
