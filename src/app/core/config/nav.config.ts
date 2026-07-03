import { UserRole } from '../../shared/models/auth.models';

/**
 * Describes a single sidebar navigation entry.
 *
 * - `roles`    empty array means ALL authenticated roles can see it.
 * - `readOnly` roles listed here see the item but with a lock badge and
 *              the route guard will allow read-only access only.
 * - `badge`    optional static badge text (e.g. "New").
 */
export interface NavItem {
  label:    string;
  icon:     string;
  route:    string;
  /** Roles that can see AND access this item. Empty = all roles. */
  roles:    UserRole[];
  /** Subset of `roles` that have read-only access (item visible but tagged). */
  readOnly?: UserRole[];
  badge?:   string;
  /** Visual divider above this item */
  divider?: boolean;
  /** Indent as a child item (admin sub-pages) */
  child?:   boolean;
}

/**
 * Master nav config — single source of truth for both sidebar rendering
 * and route-level RBAC.
 */
export const NAV_ITEMS: NavItem[] = [
  // ── All roles ─────────────────────────────────────────────────────────────
  {
    label: 'Dashboard',
    icon:  'dashboard',
    route: '/dashboard',
    roles: [],
  },

  // ── Viewer + Manager + Admin ──────────────────────────────────────────────
  {
    label:    'Units',
    icon:     'apartment',
    route:    '/units',
    roles:    ['Admin', 'Manager', 'Viewer'],
    readOnly: ['Viewer'],
  },
  {
    label: 'Tenants',
    icon:  'people',
    route: '/tenants',
    roles: ['Admin', 'Manager'],
  },
  {
    label:    'Leases',
    icon:     'description',
    route:    '/leases',
    roles:    ['Admin', 'Manager', 'Viewer'],
    readOnly: ['Viewer'],
  },
  {
    label:    'Invoices',
    icon:     'receipt_long',
    route:    '/invoices',
    roles:    ['Admin', 'Manager', 'Viewer'],
    readOnly: ['Viewer'],
  },
  {
    label: 'Payments',
    icon:  'payments',
    route: '/payments',
    roles: ['Admin', 'Manager'],
  },
  {
    label: 'Reports',
    icon:  'bar_chart',
    route: '/reports',
    roles: ['Admin', 'Manager'],
  },

  // ── All roles ─────────────────────────────────────────────────────────────
  {
    label: 'Notifications',
    icon:  'notifications',
    route: '/notifications',
    roles: [],
  },

  // ── Admin only ────────────────────────────────────────────────────────────
  {
    label:   'Admin',
    icon:    'admin_panel_settings',
    route:   '/admin',
    roles:   ['Admin'],
    divider: true,
  },
  {
    label: 'User Management',
    icon:  'manage_accounts',
    route: '/admin/users',
    roles: ['Admin'],
    child: true,
  },
  {
    label: 'Register Manager',
    icon:  'person_add',
    route: '/admin/register-manager',
    roles: ['Admin'],
    child: true,
  },
  {
    label: 'System Settings',
    icon:  'settings',
    route: '/admin/settings',
    roles: ['Admin'],
    child: true,
  },
];
