import { Routes } from '@angular/router';
import { authGuard }   from './core/guards/auth.guard';
import { noAuthGuard } from './core/guards/no-auth.guard';
import { roleGuard }   from './core/guards/role.guard';

export const routes: Routes = [

  // ── Root redirect ──────────────────────────────────────────────────────────
  { path: '', redirectTo: 'dashboard', pathMatch: 'full' },

  // ── Public: auth pages (blocked if already logged in) ─────────────────────
  {
    path: 'auth',
    loadComponent: () =>
      import('./features/auth/auth-layout/auth-layout.component')
        .then(m => m.AuthLayoutComponent),
    canActivate: [noAuthGuard],
    children: [
      { path: '', redirectTo: 'login', pathMatch: 'full' },
      {
        path: 'login',
        loadComponent: () =>
          import('./features/auth/login/login.component').then(m => m.LoginComponent),
        title: 'Sign In — BMS',
      },
      {
        path: 'register',
        loadComponent: () =>
          import('./features/auth/register/register.component').then(m => m.RegisterComponent),
        title: 'Create Account — BMS',
      },
    ],
  },

  // ── Access-denied page ─────────────────────────────────────────────────────
  {
    path: 'forbidden',
    loadComponent: () =>
      import('./shared/components/forbidden/forbidden.component')
        .then(m => m.ForbiddenComponent),
    title: 'Access Denied — BMS',
  },

  // ── Authenticated app shell ────────────────────────────────────────────────
  {
    path: '',
    loadComponent: () =>
      import('./shared/layout/shell/shell.component').then(m => m.ShellComponent),
    canActivate: [authGuard],
    children: [

      // Dashboard — all roles
      {
        path: 'dashboard',
        loadComponent: () =>
          import('./features/dashboard/dashboard.component').then(m => m.DashboardComponent),
        title: 'Dashboard — BMS',
      },

      // Units — Viewer (read-only), Manager, Admin
      {
        path: 'units',
        loadComponent: () =>
          import('./features/units/units.component').then(m => m.UnitsComponent),
        canActivate: [roleGuard('Admin', 'Manager', 'Viewer')],
        title: 'Units — BMS',
      },

      // Tenants — Manager, Admin only
      {
        path: 'tenants',
        loadComponent: () =>
          import('./features/tenants/tenants.component').then(m => m.TenantsComponent),
        canActivate: [roleGuard('Admin', 'Manager')],
        title: 'Tenants — BMS',
      },

      // Leases — Viewer (read-only), Manager, Admin
      {
        path: 'leases',
        loadComponent: () =>
          import('./features/leases/leases.component').then(m => m.LeasesComponent),
        canActivate: [roleGuard('Admin', 'Manager', 'Viewer')],
        title: 'Leases — BMS',
      },

      // Invoices — Viewer (read-only), Manager, Admin
      {
        path: 'invoices',
        loadComponent: () =>
          import('./features/invoices/invoices.component').then(m => m.InvoicesComponent),
        canActivate: [roleGuard('Admin', 'Manager', 'Viewer')],
        title: 'Invoices — BMS',
      },

      // Payments — Manager, Admin only
      {
        path: 'payments',
        loadComponent: () =>
          import('./features/payments/payments.component').then(m => m.PaymentsComponent),
        canActivate: [roleGuard('Admin', 'Manager')],
        title: 'Payments — BMS',
      },

      // Reports — Manager, Admin only
      {
        path: 'reports',
        loadComponent: () =>
          import('./features/reports/reports.component').then(m => m.ReportsComponent),
        canActivate: [roleGuard('Admin', 'Manager')],
        title: 'Reports — BMS',
      },

      // Notifications — all roles
      {
        path: 'notifications',
        loadComponent: () =>
          import('./features/notifications/notifications.component')
            .then(m => m.NotificationsComponent),
        title: 'Notifications — BMS',
      },

      // ── Admin section — Admin only ─────────────────────────────────────────
      {
        path: 'admin',
        canActivate: [roleGuard('Admin')],
        children: [
          {
            path: '',
            loadComponent: () =>
              import('./features/admin/admin.component').then(m => m.AdminComponent),
            title: 'Admin — BMS',
          },
          {
            path: 'users',
            loadComponent: () =>
              import('./features/admin/user-management/user-management.component')
                .then(m => m.UserManagementComponent),
            title: 'User Management — BMS',
          },
          {
            path: 'register-manager',
            loadComponent: () =>
              import('./features/admin/register-manager/register-manager.component')
                .then(m => m.RegisterManagerComponent),
            title: 'Register Manager — BMS',
          },
          {
            path: 'settings',
            loadComponent: () =>
              import('./features/admin/system-settings/system-settings.component')
                .then(m => m.SystemSettingsComponent),
            title: 'System Settings — BMS',
          },
        ],
      },
    ],
  },

  // ── Catch-all ──────────────────────────────────────────────────────────────
  { path: '**', redirectTo: 'dashboard' },
];
