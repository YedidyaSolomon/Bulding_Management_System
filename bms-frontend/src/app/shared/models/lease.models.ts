// ── Lease DTOs — mirrors backend BMS.Application.DTOs.Leases exactly ─────────

export interface LeaseDto {
  id:                number;
  unitId:            number;
  unitNumber:        string;
  tenantId:          number;
  tenantName:        string;
  startDate:         string;   // ISO date string
  endDate:           string;   // ISO date string
  monthlyRent:       number;
  depositAmount:     number;
  status:            string;
  terminationReason: string | null;
}

export interface CreateLeaseDto {
  unitId:        number;
  tenantId:      number;
  startDate:     string;
  endDate:       string;
  monthlyRent:   number;
  depositAmount: number;
}

export interface UpdateLeaseDto {
  endDate:       string;
  monthlyRent:   number;
  depositAmount: number;
  // status intentionally omitted — derived server-side by ComputeStatus()
}

export interface TerminateLeaseDto {
  reason: string;
}

// ── Constants ────────────────────────────────────────────────────────────────

export const LEASE_STATUSES = [
  'Active',
  'Expired',
  'Terminated',
  'PendingRenewal',
] as const;

export type LeaseStatus = typeof LEASE_STATUSES[number];

// ── Display helpers ──────────────────────────────────────────────────────────

export const LEASE_STATUS_LABELS: Record<string, string> = {
  Active:          'Active',
  Expired:         'Expired',
  Terminated:      'Terminated',
  PendingRenewal:  'Pending Renewal',
};

export const LEASE_STATUS_CLASSES: Record<string, string> = {
  Active:         'badge-success',
  Expired:        'badge-neutral',
  Terminated:     'badge-error',
  PendingRenewal: 'badge-warning',
};
