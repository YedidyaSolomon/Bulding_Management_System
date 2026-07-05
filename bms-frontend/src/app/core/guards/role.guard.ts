import { inject } from '@angular/core';
import { CanActivateFn, Router } from '@angular/router';
import { AuthService } from '../services/auth.service';

/**
 * Role-based access control guard.
 * Usage in route config:
 *   canActivate: [authGuard, roleGuard('Admin', 'Manager')]
 */
export const roleGuard = (...allowedRoles: string[]): CanActivateFn => {
  return () => {
    const authService = inject(AuthService);
    const router      = inject(Router);

    if (authService.hasAnyRole(...allowedRoles)) {
      return true;
    }

    return router.createUrlTree(['/forbidden']);
  };
};
