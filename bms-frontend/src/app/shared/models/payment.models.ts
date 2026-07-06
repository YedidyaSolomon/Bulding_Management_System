// ── Payment DTOs — mirrors BMS.Application.DTOs.Payments exactly ─────────────

export interface PaymentDto {
  id:              number;
  invoiceId:       number;
  invoiceNumber:   string;
  amountPaid:      number;
  paymentDate:     string;   // ISO date string
  paymentMethod:   string;
  referenceNumber: string;
  notes:           string | null;
}

export interface CreatePaymentDto {
  invoiceId:       number;
  amountPaid:      number;
  paymentDate:     string;   // ISO datetime string (YYYY-MM-DDT00:00:00.000Z)
  paymentMethod:   string;
  referenceNumber: string;
  notes:           string | null;
}

// ── Constants ────────────────────────────────────────────────────────────────

export const PAYMENT_METHODS = [
  'Cash',
  'BankTransfer',
  'Cheque',
  'MobileMoney',
] as const;

export type PaymentMethod = typeof PAYMENT_METHODS[number];

// ── Display helpers ──────────────────────────────────────────────────────────

export const PAYMENT_METHOD_LABELS: Record<string, string> = {
  Cash:         'Cash',
  BankTransfer: 'Bank Transfer',
  Cheque:       'Cheque',
  MobileMoney:  'Mobile Money',
};

export const PAYMENT_METHOD_ICONS: Record<string, string> = {
  Cash:         'payments',
  BankTransfer: 'account_balance',
  Cheque:       'receipt',
  MobileMoney:  'smartphone',
};
