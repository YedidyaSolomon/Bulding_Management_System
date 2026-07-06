// ─────────────────────────────────────────────────────────────────────────────
// Report DTOs — mirror the BMS backend report response shapes exactly.
// All monetary values are in ETB (Ethiopian Birr).
// ─────────────────────────────────────────────────────────────────────────────

// ── Occupancy ────────────────────────────────────────────────────────────────

export interface FloorOccupancy {
  floorNumber:   number;
  totalUnits:    number;
  occupiedUnits: number;
  vacantUnits:   number;
}

export interface OccupancyReport {
  totalUnits:      number;
  occupiedUnits:   number;
  vacantUnits:     number;
  occupancyRate:   number;        // 0–100 percentage
  byFloor:         FloorOccupancy[];
}

// ── Revenue ──────────────────────────────────────────────────────────────────

export interface MonthlyRevenue {
  year:              number;
  month:             number;      // 1–12
  expectedRevenue:   number;
  collectedRevenue:  number;
  outstandingAmount: number;
}

export interface RevenueReport {
  collectedThisMonth: number;
  expectedThisMonth:  number;
  yearToDate:         number;
  collectionRate:     number;     // 0–100 percentage
  monthly:            MonthlyRevenue[];
}

// ── Arrears ───────────────────────────────────────────────────────────────────

export interface TenantArrear {
  tenantId:        number;
  tenantName:      string;
  unitNumber:      string;
  overdueInvoices: number;
  totalOwed:       number;
  oldestDueDays:   number;        // days since oldest unpaid invoice was due
}

export interface ArrearsReport {
  totalOverdue:       number;     // ETB
  tenantsInArrears:   number;
  overdueInvoices:    number;
  arrears:            TenantArrear[];
}

// ── Lease Expiry ──────────────────────────────────────────────────────────────

export interface ExpiringLease {
  leaseId:       number;
  unitNumber:    string;
  tenantName:    string;
  endDate:       string;          // ISO date string
  daysRemaining: number;
  monthlyRent:   number;
}

export interface LeaseExpiryReport {
  expiringCount: number;
  leases:        ExpiringLease[];
}

// ── Document Expiry ───────────────────────────────────────────────────────────

export interface ExpiringDocument {
  documentId:    number;
  tenantName:    string;
  documentType:  string;
  expiryDate:    string;          // ISO date string
  daysRemaining: number;
}

export interface DocumentExpiryReport {
  expiringCount: number;
  documents:     ExpiringDocument[];
}

// ── Shared wrapper (matches ApiResponse<T> used everywhere in the app) ────────

export interface ApiResponse<T> {
  success: boolean;
  data:    T | null;
}
