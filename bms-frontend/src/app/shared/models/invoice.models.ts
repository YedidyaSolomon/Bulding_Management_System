// ── Invoice DTOs — mirrors BMS.Application.DTOs.Invoices exactly ─────────────

export interface InvoiceDto {
  id:            number;
  leaseId:       number;
  invoiceNumber: string;
  amountDue:     number;
  dueDate:       string;   // ISO date string
  issueDate:     string;   // ISO date string
  status:        string;
  periodMonth:   number;
  periodYear:    number;
  // Denormalised display fields
  tenantName:    string;
  unitNumber:    string;
}

export interface CreateInvoiceDto {
  leaseId:     number;
  amountDue:   number;
  dueDate:     string;   // ISO datetime string (YYYY-MM-DDT00:00:00.000Z)
  periodMonth: number;
  periodYear:  number;
}

// ── Constants ────────────────────────────────────────────────────────────────

export const INVOICE_STATUSES = [
  'Draft',
  'Issued',
  'Paid',
  'Overdue',
  'Cancelled',
] as const;

export type InvoiceStatus = typeof INVOICE_STATUSES[number];

export const MONTH_NAMES = [
  'January', 'February', 'March',     'April',   'May',      'June',
  'July',    'August',   'September', 'October', 'November', 'December',
];

// ── Display helpers ──────────────────────────────────────────────────────────

export const INVOICE_STATUS_LABELS: Record<string, string> = {
  Draft:     'Draft',
  Issued:    'Issued',
  Paid:      'Paid',
  Overdue:   'Overdue',
  Cancelled: 'Cancelled',
};

export const INVOICE_STATUS_CLASSES: Record<string, string> = {
  Draft:     'badge-neutral',
  Issued:    'badge-info',
  Paid:      'badge-success',
  Overdue:   'badge-danger',
  Cancelled: 'badge-neutral',
};
