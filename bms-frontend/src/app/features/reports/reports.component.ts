import {
  Component, OnInit, OnDestroy, NgZone, ChangeDetectorRef,
} from '@angular/core';
import { CommonModule } from '@angular/common';
import { ReactiveFormsModule, FormControl } from '@angular/forms';
import { MatIconModule } from '@angular/material/icon';
import { MatButtonModule } from '@angular/material/button';
import { MatTooltipModule } from '@angular/material/tooltip';
import { MatProgressSpinnerModule } from '@angular/material/progress-spinner';
import { MatSelectModule } from '@angular/material/select';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatSortModule, Sort } from '@angular/material/sort';
import { BaseChartDirective } from 'ng2-charts';
import { ChartData, ChartOptions } from 'chart.js';
import { Subject } from 'rxjs';
import { takeUntil, finalize } from 'rxjs/operators';

import { ReportService, ReportType } from './report.service';
import { EtbCurrencyPipe } from '../../shared/pipes/etb-currency.pipe';
import {
  OccupancyReport,
  RevenueReport,
  ArrearsReport,
  TenantArrear,
  LeaseExpiryReport,
  ExpiringLease,
  DocumentExpiryReport,
  ExpiringDocument,
} from './report.models';

// ── Brand colours ─────────────────────────────────────────────────────────────
const C_INDIGO  = '#667eea';
const C_PURPLE  = '#764ba2';
const C_GREEN   = '#22c55e';
const C_GREEN_L = '#86efac';
const C_AMBER   = '#f59e0b';
const C_RED     = '#ef4444';
const C_BLUE    = '#3b82f6';
const C_MUTED   = '#e2e8f0';

export type TabId = 'occupancy' | 'revenue' | 'arrears' | 'lease-expiry' | 'document-expiry';

interface Tab { id: TabId; label: string; icon: string; }

// ─────────────────────────────────────────────────────────────────────────────

@Component({
  selector: 'app-reports',
  standalone: true,
  imports: [
    CommonModule,
    ReactiveFormsModule,
    MatIconModule,
    MatButtonModule,
    MatTooltipModule,
    MatProgressSpinnerModule,
    MatSelectModule,
    MatFormFieldModule,
    MatSortModule,
    BaseChartDirective,
    EtbCurrencyPipe,
  ],
  templateUrl: './reports.component.html',
  styleUrls:  ['./reports.component.scss'],
})
export class ReportsComponent implements OnInit, OnDestroy {

  // ── Tabs ──────────────────────────────────────────────────────────────────
  readonly tabs: Tab[] = [
    { id: 'occupancy',        label: 'Occupancy',        icon: 'apartment'       },
    { id: 'revenue',          label: 'Revenue',          icon: 'trending_up'     },
    { id: 'arrears',          label: 'Arrears',          icon: 'warning_amber'   },
    { id: 'lease-expiry',     label: 'Lease Expiry',     icon: 'event_busy'      },
    { id: 'document-expiry',  label: 'Document Expiry',  icon: 'folder_off'      },
  ];
  activeTab: TabId = 'occupancy';

  // ── Shared state per report ───────────────────────────────────────────────
  loading: Record<TabId, boolean> = {
    'occupancy': false, 'revenue': false, 'arrears': false,
    'lease-expiry': false, 'document-expiry': false,
  };
  error: Record<TabId, string | null> = {
    'occupancy': null, 'revenue': null, 'arrears': null,
    'lease-expiry': null, 'document-expiry': null,
  };
  exporting = false;

  // ── Report data ───────────────────────────────────────────────────────────
  occupancy:       OccupancyReport   | null = null;
  revenue:         RevenueReport     | null = null;
  arrears:         ArrearsReport     | null = null;
  leaseExpiry:     LeaseExpiryReport | null = null;
  documentExpiry:  DocumentExpiryReport | null = null;

  // ── Filters ───────────────────────────────────────────────────────────────
  leaseExpiryDays     = new FormControl<number>(30);
  documentExpiryDays  = new FormControl<number>(30);
  readonly dayOptions = [7, 14, 30, 60, 90];

  // ── Arrears table sort ────────────────────────────────────────────────────
  arrearsSortField:  keyof TenantArrear = 'totalOwed';
  arrearsSortDir:    'asc' | 'desc'     = 'desc';

  // ── Animated KPI display values ───────────────────────────────────────────
  anim: Record<string, number> = {};

  // ── Chart data ────────────────────────────────────────────────────────────
  occupancyChartData:  ChartData<'doughnut'> | null = null;
  occupancyFloorData:  ChartData<'bar'>      | null = null;
  revenueChartData:    ChartData<'bar'>      | null = null;

  readonly occupancyChartOpts: ChartOptions<'doughnut'> = {
    responsive: true,
    maintainAspectRatio: false,
    plugins: {
      legend: { position: 'bottom', labels: { padding: 16, font: { size: 13 } } },
      tooltip: { callbacks: { label: ctx => ` ${ctx.label}: ${ctx.parsed} units` } },
    },
    cutout: '68%',
  };

  readonly floorBarOpts: ChartOptions<'bar'> = {
    responsive: true,
    maintainAspectRatio: false,
    plugins: { legend: { position: 'bottom', labels: { font: { size: 12 } } } },
    scales: {
      x: { stacked: true, grid: { display: false }, ticks: { font: { size: 12 } } },
      y: { stacked: true, beginAtZero: true, ticks: { stepSize: 1, font: { size: 12 } } },
    },
  };

  readonly revenueBarOpts: ChartOptions<'bar'> = {
    responsive: true,
    maintainAspectRatio: false,
    plugins: { legend: { position: 'bottom', labels: { font: { size: 12 } } } },
    scales: {
      x: { grid: { display: false }, ticks: { font: { size: 11 } } },
      y: { beginAtZero: true, ticks: { font: { size: 12 } } },
    },
  };

  private destroy$ = new Subject<void>();
  // Track which tabs have been loaded so we don't reload on every tab switch
  private loaded = new Set<TabId>();

  constructor(
    private reportService: ReportService,
    private ngZone:  NgZone,
    private cdr:     ChangeDetectorRef,
  ) {}

  ngOnInit(): void {
    this.loadTab('occupancy');

    // Reload lease/document expiry when day filter changes
    this.leaseExpiryDays.valueChanges
      .pipe(takeUntil(this.destroy$))
      .subscribe(() => this.loadTab('lease-expiry', true));

    this.documentExpiryDays.valueChanges
      .pipe(takeUntil(this.destroy$))
      .subscribe(() => this.loadTab('document-expiry', true));
  }

  ngOnDestroy(): void {
    this.destroy$.next();
    this.destroy$.complete();
  }

  // ── Tab switching ─────────────────────────────────────────────────────────

  selectTab(id: TabId): void {
    this.activeTab = id;
    if (!this.loaded.has(id)) this.loadTab(id);
  }

  // ── Data loading ──────────────────────────────────────────────────────────

  loadTab(id: TabId, force = false): void {
    if (this.loaded.has(id) && !force) return;
    this.loading[id] = true;
    this.error[id]   = null;

    switch (id) {
      case 'occupancy':
        this.reportService.getOccupancy()
          .pipe(takeUntil(this.destroy$), finalize(() => { this.loading[id] = false; this.cdr.markForCheck(); }))
          .subscribe({
            next: d => {
              this.occupancy = d;
              this.buildOccupancyCharts(d);
              this.animateKpis({ occRate: d.occupancyRate, occOcc: d.occupiedUnits, occVac: d.vacantUnits, occTotal: d.totalUnits });
              this.loaded.add(id);
            },
            error: () => { this.error[id] = 'Failed to load occupancy report.'; },
          });
        break;

      case 'revenue':
        this.reportService.getRevenue()
          .pipe(takeUntil(this.destroy$), finalize(() => { this.loading[id] = false; this.cdr.markForCheck(); }))
          .subscribe({
            next: d => {
              this.revenue = d;
              this.buildRevenueChart(d);
              this.animateKpis({ revCol: d.collectedThisMonth, revExp: d.expectedThisMonth, revYtd: d.yearToDate, revRate: d.collectionRate });
              this.loaded.add(id);
            },
            error: () => { this.error[id] = 'Failed to load revenue report.'; },
          });
        break;

      case 'arrears':
        this.reportService.getArrears()
          .pipe(takeUntil(this.destroy$), finalize(() => { this.loading[id] = false; this.cdr.markForCheck(); }))
          .subscribe({
            next: d => {
              this.arrears = d;
              this.animateKpis({ arrOwed: d.totalOverdue, arrTen: d.tenantsInArrears, arrInv: d.overdueInvoices });
              this.loaded.add(id);
            },
            error: () => { this.error[id] = 'Failed to load arrears report.'; },
          });
        break;

      case 'lease-expiry':
        this.reportService.getLeaseExpiry(this.leaseExpiryDays.value ?? 30)
          .pipe(takeUntil(this.destroy$), finalize(() => { this.loading[id] = false; this.cdr.markForCheck(); }))
          .subscribe({
            next: d => {
              this.leaseExpiry = d;
              this.loaded.add(id);
            },
            error: () => { this.error[id] = 'Failed to load lease expiry report.'; },
          });
        break;

      case 'document-expiry':
        this.reportService.getDocumentExpiry(this.documentExpiryDays.value ?? 30)
          .pipe(takeUntil(this.destroy$), finalize(() => { this.loading[id] = false; this.cdr.markForCheck(); }))
          .subscribe({
            next: d => {
              this.documentExpiry = d;
              this.loaded.add(id);
            },
            error: () => { this.error[id] = 'Failed to load document expiry report.'; },
          });
        break;
    }
  }

  retry(id: TabId): void { this.loaded.delete(id); this.loadTab(id); }

  // ── Chart builders ────────────────────────────────────────────────────────

  private buildOccupancyCharts(d: OccupancyReport): void {
    this.occupancyChartData = {
      labels: ['Occupied', 'Vacant'],
      datasets: [{
        data: [d.occupiedUnits, d.vacantUnits],
        backgroundColor:      [C_GREEN,   C_MUTED],
        hoverBackgroundColor: [C_GREEN_L, '#cbd5e1'],
        borderWidth: 0,
      }],
    };

    const floors = [...d.byFloor].sort((a, b) => a.floorNumber - b.floorNumber);
    this.occupancyFloorData = {
      labels: floors.map(f => `Floor ${f.floorNumber}`),
      datasets: [
        {
          label: 'Occupied',
          data:  floors.map(f => f.occupiedUnits),
          backgroundColor: C_GREEN,
          borderRadius: 4,
        },
        {
          label: 'Vacant',
          data:  floors.map(f => f.vacantUnits),
          backgroundColor: C_MUTED,
          borderRadius: 4,
        },
      ],
    };
  }

  private buildRevenueChart(d: RevenueReport): void {
    const MONTHS = ['Jan','Feb','Mar','Apr','May','Jun','Jul','Aug','Sep','Oct','Nov','Dec'];
    const rows = d.monthly.slice(-6);  // last 6 months
    this.revenueChartData = {
      labels: rows.map(r => `${MONTHS[r.month - 1]} ${r.year}`),
      datasets: [
        {
          label:           'Collected',
          data:            rows.map(r => r.collectedRevenue),
          backgroundColor: C_GREEN,
          borderRadius:    6,
        },
        {
          label:           'Expected',
          data:            rows.map(r => r.expectedRevenue),
          backgroundColor: C_BLUE,
          borderRadius:    6,
        },
      ],
    };
  }

  // ── Count-up animation ────────────────────────────────────────────────────

  private animateKpis(targets: Record<string, number>, durationMs = 700): void {
    const steps  = 40;
    const stepMs = durationMs / steps;

    Object.keys(targets).forEach(key => {
      const target = targets[key];
      let current  = 0;

      this.ngZone.runOutsideAngular(() => {
        const iv = setInterval(() => {
          current++;
          const eased = 1 - Math.pow(1 - current / steps, 3);
          const val   = current >= steps ? target : Math.round(target * eased * 100) / 100;
          if (current >= steps) clearInterval(iv);
          this.ngZone.run(() => { this.anim[key] = val; this.cdr.markForCheck(); });
        }, stepMs);
      });
    });
  }

  // ── Arrears sort ──────────────────────────────────────────────────────────

  get sortedArrears(): TenantArrear[] {
    if (!this.arrears) return [];
    return [...this.arrears.arrears].sort((a, b) => {
      const va = a[this.arrearsSortField] as number | string;
      const vb = b[this.arrearsSortField] as number | string;
      const cmp = va < vb ? -1 : va > vb ? 1 : 0;
      return this.arrearsSortDir === 'asc' ? cmp : -cmp;
    });
  }

  onArrearsSort(sort: Sort): void {
    this.arrearsSortField = (sort.active as keyof TenantArrear) || 'totalOwed';
    this.arrearsSortDir   = (sort.direction || 'desc') as 'asc' | 'desc';
  }

  // ── Expiry badge ──────────────────────────────────────────────────────────

  expiryClass(days: number): string {
    if (days <= 7)  return 'badge-danger';
    if (days <= 30) return 'badge-warning';
    return 'badge-success';
  }

  expiryLabel(days: number): string {
    if (days <= 0) return 'Expired';
    if (days === 1) return '1 day';
    return `${days} days`;
  }

  // ── Export ────────────────────────────────────────────────────────────────

  exportExcel(): void {
    if (this.exporting) return;
    this.exporting = true;

    const typeMap: Record<TabId, ReportType> = {
      'occupancy':       'occupancy',
      'revenue':         'revenue',
      'arrears':         'arrears',
      'lease-expiry':    'lease-expiry',
      'document-expiry': 'document-expiry',
    };

    this.reportService.exportToExcel(typeMap[this.activeTab])
      .pipe(finalize(() => { this.exporting = false; this.cdr.markForCheck(); }))
      .subscribe({
        next: blob => {
          const url  = URL.createObjectURL(blob);
          const link = document.createElement('a');
          link.href     = url;
          link.download = `${this.activeTab}-report.xlsx`;
          link.click();
          URL.revokeObjectURL(url);
        },
        error: () => {},
      });
  }

  // ── Helpers ───────────────────────────────────────────────────────────────

  get activeTabLabel(): string {
    return this.tabs.find(t => t.id === this.activeTab)?.label ?? '';
  }

  trackByIndex(i: number): number { return i; }
  trackById(i: number, item: { tenantId?: number; leaseId?: number; documentId?: number }): number {
    return item.tenantId ?? item.leaseId ?? item.documentId ?? i;
  }

  leaseSortedList(leases: ExpiringLease[]): ExpiringLease[] {
    return [...leases].sort((a, b) => a.daysRemaining - b.daysRemaining);
  }

  docSortedList(docs: ExpiringDocument[]): ExpiringDocument[] {
    return [...docs].sort((a, b) => a.daysRemaining - b.daysRemaining);
  }
}
