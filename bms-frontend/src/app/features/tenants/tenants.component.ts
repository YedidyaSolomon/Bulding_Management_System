import { Component, OnInit, OnDestroy } from '@angular/core';
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
import { MatChipsModule } from '@angular/material/chips';
import { ViewChild } from '@angular/core';
import { Subject } from 'rxjs';
import { debounceTime, distinctUntilChanged, takeUntil } from 'rxjs/operators';

import { AuthService } from '../../core/services/auth.service';
import { TenantService } from './tenant.service';
import { TenantDto, BUSINESS_TYPES } from '../../shared/models/tenant.models';
import { TenantCardComponent } from './tenant-card/tenant-card.component';
import { TenantFormDialogComponent } from './tenant-form-dialog/tenant-form-dialog.component';
import { TenantDeleteDialogComponent } from './tenant-delete-dialog/tenant-delete-dialog.component';
import { TenantDetailDialogComponent } from './tenant-detail-dialog/tenant-detail-dialog.component';

@Component({
  selector: 'app-tenants',
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
    MatChipsModule,
    TenantCardComponent,
  ],
  templateUrl: './tenants.component.html',
  styleUrls: ['./tenants.component.scss'],
})
export class TenantsComponent implements OnInit, OnDestroy {

  @ViewChild(MatPaginator) paginator!: MatPaginator;

  allTenants:      TenantDto[] = [];
  displayedTenants: TenantDto[] = [];
  loading = false;

  // Filters
  searchControl       = new FormControl('');
  statusFilter        = new FormControl('all');   // 'all' | 'active' | 'inactive'
  businessTypeFilter  = new FormControl('all');
  sortControl         = new FormControl('name-asc');

  readonly businessTypes = BUSINESS_TYPES;

  // Pagination
  pageSize        = 12;
  pageIndex       = 0;
  totalCount      = 0;
  pageSizeOptions = [6, 12, 24, 48];

  private destroy$ = new Subject<void>();

  constructor(
    private tenantService: TenantService,
    private authService:   AuthService,
    private dialog:        MatDialog,
  ) {}

  get role(): string          { return this.authService.getRole() ?? ''; }
  get canCreate(): boolean    { return this.role === 'Admin' || this.role === 'Manager'; }
  get canEdit(): boolean      { return this.role === 'Admin' || this.role === 'Manager'; }
  get canDelete(): boolean    { return this.role === 'Admin'; }

  ngOnInit(): void {
    this.loadTenants();

    this.searchControl.valueChanges
      .pipe(debounceTime(300), distinctUntilChanged(), takeUntil(this.destroy$))
      .subscribe(() => { this.pageIndex = 0; this.applyFilters(); });

    this.statusFilter.valueChanges
      .pipe(takeUntil(this.destroy$))
      .subscribe(() => { this.pageIndex = 0; this.applyFilters(); });

    this.businessTypeFilter.valueChanges
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

  loadTenants(): void {
    this.loading = true;
    this.tenantService.getAll()
      .pipe(takeUntil(this.destroy$))
      .subscribe({
        next: tenants => {
          this.allTenants = tenants;
          this.applyFilters();
          this.loading = false;
        },
        error: () => { this.loading = false; },
      });
  }

  applyFilters(): void {
    const search  = (this.searchControl.value ?? '').toLowerCase().trim();
    const status  = this.statusFilter.value ?? 'all';
    const btype   = this.businessTypeFilter.value ?? 'all';

    let filtered = this.allTenants.filter(t => {
      const matchSearch =
        !search ||
        t.organizationName.toLowerCase().includes(search)  ||
        t.contactPersonName.toLowerCase().includes(search) ||
        t.contactEmail.toLowerCase().includes(search)      ||
        t.tin.toLowerCase().includes(search)               ||
        t.phone.toLowerCase().includes(search);

      const matchStatus =
        status === 'all'     ? true :
        status === 'active'  ? t.isActive :
                               !t.isActive;

      const matchType = btype === 'all' || t.businessType === btype;

      return matchSearch && matchStatus && matchType;
    });

    this.totalCount = filtered.length;

    // Sort
    const [field, dir] = (this.sortControl.value ?? 'name-asc').split('-');
    filtered = this.sortData(filtered, field, dir);

    // Paginate
    const start = this.pageIndex * this.pageSize;
    this.displayedTenants = filtered.slice(start, start + this.pageSize);
  }

  onPageChange(event: PageEvent): void {
    this.pageIndex = event.pageIndex;
    this.pageSize  = event.pageSize;
    this.applyFilters();
  }

  clearFilters(): void {
    this.searchControl.setValue('');
    this.statusFilter.setValue('all');
    this.businessTypeFilter.setValue('all');
    this.pageIndex = 0;
  }

  get hasActiveFilters(): boolean {
    return (
      !!this.searchControl.value     ||
      this.statusFilter.value       !== 'all' ||
      this.businessTypeFilter.value !== 'all'
    );
  }

  get activeTenantCount():   number { return this.allTenants.filter(t =>  t.isActive).length; }
  get inactiveTenantCount(): number { return this.allTenants.filter(t => !t.isActive).length; }

  // ── Dialogs ────────────────────────────────────────────────────────────────

  openCreate(): void {
    this.dialog.open(TenantFormDialogComponent, {
      width: '620px', maxWidth: '95vw', data: {},
    }).afterClosed().subscribe(result => {
      if (result) this.loadTenants();
    });
  }

  openEdit(tenant: TenantDto): void {
    this.dialog.open(TenantFormDialogComponent, {
      width: '620px', maxWidth: '95vw', data: { tenant },
    }).afterClosed().subscribe(result => {
      if (result) this.loadTenants();
    });
  }

  openDelete(tenant: TenantDto): void {
    this.dialog.open(TenantDeleteDialogComponent, {
      width: '440px', maxWidth: '95vw', data: { tenant },
    }).afterClosed().subscribe(deleted => {
      if (deleted) this.loadTenants();
    });
  }

  openDetail(tenant: TenantDto): void {
    this.dialog.open(TenantDetailDialogComponent, {
      width: '600px', maxWidth: '95vw', data: { tenant },
    }).afterClosed().subscribe(() => {
      // Reload in case a link-user action happened inside the detail dialog
      this.loadTenants();
    });
  }

  // ── Sort helper ────────────────────────────────────────────────────────────

  private sortData(data: TenantDto[], field: string, dir: string): TenantDto[] {
    if (!field || !dir) return data;
    return [...data].sort((a, b) => {
      let valA: string | number;
      let valB: string | number;
      switch (field) {
        case 'name':  valA = a.organizationName;  valB = b.organizationName;  break;
        case 'type':  valA = a.businessType;       valB = b.businessType;      break;
        case 'email': valA = a.contactEmail;       valB = b.contactEmail;      break;
        default:      return 0;
      }
      const cmp = valA < valB ? -1 : valA > valB ? 1 : 0;
      return dir === 'asc' ? cmp : -cmp;
    });
  }
}
