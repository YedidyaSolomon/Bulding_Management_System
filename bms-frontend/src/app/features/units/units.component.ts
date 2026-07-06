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
import { MatChipsModule } from '@angular/material/chips';
import { Subject } from 'rxjs';
import { debounceTime, distinctUntilChanged, takeUntil } from 'rxjs/operators';

import { AuthService } from '../../core/services/auth.service';
import { UnitService } from './unit.service';
import { UnitDto, UNIT_TYPES, UNIT_STATUSES } from '../../shared/models/unit.models';
import { UnitFormDialogComponent } from './unit-form-dialog/unit-form-dialog.component';
import { UnitDeleteDialogComponent } from './unit-delete-dialog/unit-delete-dialog.component';
import { UnitDetailDialogComponent } from './unit-detail-dialog/unit-detail-dialog.component';
import { UnitCardComponent } from './unit-card/unit-card.component';

@Component({
  selector: 'app-units',
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
    UnitCardComponent,
  ],
  templateUrl: './units.component.html',
  styleUrls: ['./units.component.scss'],
})
export class UnitsComponent implements OnInit, OnDestroy {

  @ViewChild(MatPaginator) paginator!: MatPaginator;

  displayedUnits: UnitDto[] = [];
  allUnits:       UnitDto[] = [];
  loading     = false;

  // Filters
  searchControl      = new FormControl('');
  statusFilter       = new FormControl('all');
  floorFilter        = new FormControl('all');
  typeFilter         = new FormControl('all');
  sortControl        = new FormControl('unitNumber-asc');

  readonly unitTypes    = UNIT_TYPES;
  readonly unitStatuses = UNIT_STATUSES;

  availableFloors: number[] = [];

  // Pagination state
  pageSize    = 10;
  pageIndex   = 0;
  totalCount  = 0;
  pageSizeOptions = [5, 10, 25, 50];

  private destroy$ = new Subject<void>();

  constructor(
    private unitService: UnitService,
    private authService: AuthService,
    private dialog:      MatDialog,
  ) {}

  get role(): string { return this.authService.getRole() ?? ''; }
  get canCreate(): boolean { return this.role === 'Admin' || this.role === 'Manager'; }
  get canEdit():   boolean { return this.role === 'Admin' || this.role === 'Manager'; }
  get canDelete(): boolean { return this.role === 'Admin'; }

  ngOnInit(): void {
    this.loadUnits();

    this.searchControl.valueChanges
      .pipe(debounceTime(300), distinctUntilChanged(), takeUntil(this.destroy$))
      .subscribe(() => this.applyFilters());

    this.statusFilter.valueChanges.pipe(takeUntil(this.destroy$)).subscribe(() => this.applyFilters());
    this.floorFilter.valueChanges.pipe(takeUntil(this.destroy$)).subscribe(() => this.applyFilters());
    this.typeFilter.valueChanges.pipe(takeUntil(this.destroy$)).subscribe(() => this.applyFilters());
    this.sortControl.valueChanges.pipe(takeUntil(this.destroy$)).subscribe(() => {
      this.pageIndex = 0;
      this.applyFilters();
    });
  }

  ngOnDestroy(): void {
    this.destroy$.next();
    this.destroy$.complete();
  }

  loadUnits(): void {
    this.loading = true;
    this.unitService.getAll()
      .pipe(takeUntil(this.destroy$))
      .subscribe({
        next: units => {
          this.allUnits = units;
          this.availableFloors = [...new Set(units.map(u => u.floorNumber))].sort((a, b) => a - b);
          this.applyFilters();
          this.loading = false;
        },
        error: () => {
          this.loading = false;
        },
      });
  }

  applyFilters(): void {
    const search = (this.searchControl.value ?? '').toLowerCase().trim();
    const status = this.statusFilter.value ?? 'all';
    const floor  = this.floorFilter.value  ?? 'all';
    const type   = this.typeFilter.value   ?? 'all';

    let filtered = this.allUnits.filter(u => {
      const matchSearch =
        !search ||
        u.unitNumber.toLowerCase().includes(search) ||
        u.floorNumber.toString().includes(search) ||
        u.unitType.toLowerCase().includes(search);

      const matchStatus = status === 'all' || u.status === status;
      const matchFloor  = floor  === 'all' || u.floorNumber.toString() === floor;
      const matchType   = type   === 'all' || u.unitType === type;

      return matchSearch && matchStatus && matchFloor && matchType;
    });

    this.totalCount = filtered.length;

    // Client-side sort
    const sortVal = this.sortControl.value ?? 'unitNumber-asc';
    const [sortState, sortDir] = sortVal.split('-');
    if (sortState && sortDir) {
      filtered = this.sortData(filtered, sortState, sortDir);
    }

    // Client-side pagination
    const start = this.pageIndex * this.pageSize;
    this.displayedUnits = filtered.slice(start, start + this.pageSize);
  }

  onPageChange(event: PageEvent): void {
    this.pageIndex = event.pageIndex;
    this.pageSize  = event.pageSize;
    this.applyFilters();
  }

  clearFilters(): void {
    this.searchControl.setValue('');
    this.statusFilter.setValue('all');
    this.floorFilter.setValue('all');
    this.typeFilter.setValue('all');
    this.pageIndex = 0;
  }

  get hasActiveFilters(): boolean {
    return (
      !!this.searchControl.value ||
      this.statusFilter.value !== 'all' ||
      this.floorFilter.value  !== 'all' ||
      this.typeFilter.value   !== 'all'
    );
  }

  // ── Dialogs ──────────────────────────────────────────────────────────────────

  openCreate(): void {
    // Build floor capacity map from current unit list (no extra HTTP call needed)
    const floorCapacityMap = this.unitService.buildFloorCapacityMap(this.allUnits);

    const ref = this.dialog.open(UnitFormDialogComponent, {
      width: '580px',
      maxWidth: '95vw',
      data: { floorCapacityMap },
    });
    ref.afterClosed().subscribe(result => {
      if (result) this.loadUnits();
    });
  }

  openEdit(unit: UnitDto): void {
    // Exclude the unit being edited so its own floor slot isn't double-counted
    const floorCapacityMap = this.unitService.buildFloorCapacityMap(this.allUnits, unit.id);

    const ref = this.dialog.open(UnitFormDialogComponent, {
      width: '580px',
      maxWidth: '95vw',
      data: { unit, floorCapacityMap },
    });
    ref.afterClosed().subscribe(result => {
      if (result) this.loadUnits();
    });
  }

  openDelete(unit: UnitDto): void {
    const ref = this.dialog.open(UnitDeleteDialogComponent, {
      width: '420px',
      maxWidth: '95vw',
      data: { unit },
    });
    ref.afterClosed().subscribe(deleted => {
      if (deleted) this.loadUnits();
    });
  }

  openDetail(unit: UnitDto): void {
    this.dialog.open(UnitDetailDialogComponent, {
      width: '500px',
      maxWidth: '95vw',
      data: { unit },
    });
  }

  // ── Helpers ──────────────────────────────────────────────────────────────────

  statusLabel(s: string): string {
    const map: Record<string, string> = {
      Available:        'Available',
      Occupied:         'Occupied',
      UnderMaintenance: 'Under Maintenance',
      Reserved:         'Reserved',
    };
    return map[s] ?? s;
  }

  statusClass(s: string): string {
    const map: Record<string, string> = {
      Available:        'badge-success',
      Occupied:         'badge-info',
      UnderMaintenance: 'badge-warning',
      Reserved:         'badge-neutral',
    };
    return map[s] ?? 'badge-neutral';
  }

  private sortData(data: UnitDto[], active: string, direction: string): UnitDto[] {
    if (!direction) return data;
    return [...data].sort((a, b) => {
      let valA: string | number;
      let valB: string | number;

      switch (active) {
        case 'unitNumber':  valA = a.unitNumber;  valB = b.unitNumber;  break;
        case 'floorNumber': valA = a.floorNumber; valB = b.floorNumber; break;
        case 'monthlyRent': valA = a.monthlyRent; valB = b.monthlyRent; break;
        case 'status':      valA = a.status;      valB = b.status;      break;
        default:            return 0;
      }

      const cmp = valA < valB ? -1 : valA > valB ? 1 : 0;
      return direction === 'asc' ? cmp : -cmp;
    });
  }
}
