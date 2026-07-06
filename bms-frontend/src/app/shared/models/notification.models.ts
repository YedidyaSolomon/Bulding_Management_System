// ── Notification DTOs — mirrors backend BMS.Application.DTOs.Notifications ───

export interface NotificationDto {
  id:               number;
  userId:           string;
  title:            string;
  message:          string;
  notificationType: string;
  isRead:           boolean;
  createdAt:        string;   // ISO datetime string
}

// ── Constants ────────────────────────────────────────────────────────────────

export const NOTIFICATION_TYPES = [
  'PaymentDue',
  'PaymentOverdue',
  'LeaseExpiry',
  'DocumentExpiry',
  'General',
] as const;

export type NotificationType = typeof NOTIFICATION_TYPES[number];

// ── Display helpers ──────────────────────────────────────────────────────────

export const NOTIFICATION_TYPE_LABELS: Record<string, string> = {
  PaymentDue:      'Payment Due',
  PaymentOverdue:  'Payment Overdue',
  LeaseExpiry:     'Lease Expiry',
  DocumentExpiry:  'Document Expiry',
  General:         'General',
};

export const NOTIFICATION_TYPE_ICONS: Record<string, string> = {
  PaymentDue:     'schedule',           // Clock icon for upcoming due dates
  PaymentOverdue: 'error_outline',      // Error icon for overdue items
  LeaseExpiry:    'home_work',          // Building/lease icon
  DocumentExpiry: 'description',        // Document icon
  General:        'notifications',      // Bell icon for general notifications
};

export const NOTIFICATION_TYPE_CLASSES: Record<string, string> = {
  PaymentDue:     'type-due',
  PaymentOverdue: 'type-overdue',
  LeaseExpiry:    'type-lease',
  DocumentExpiry: 'type-document',
  General:        'type-general',
};
