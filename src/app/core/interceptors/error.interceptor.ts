import { HttpInterceptorFn, HttpErrorResponse } from '@angular/common/http';
import { inject } from '@angular/core';
import { Router } from '@angular/router';
import { MatSnackBar } from '@angular/material/snack-bar';
import { catchError, throwError } from 'rxjs';
import { AuthService } from '../services/auth.service';

/**
 * Global HTTP error handler:
 *  401 → clear session and redirect to login
 *  403 → redirect to /forbidden
 *  500 → show friendly snackbar
 *  Network errors → show connectivity snackbar
 */
export const errorInterceptor: HttpInterceptorFn = (req, next) => {
  const router      = inject(Router);
  const authService = inject(AuthService);
  const snackBar    = inject(MatSnackBar);

  return next(req).pipe(
    catchError((error: HttpErrorResponse) => {
      switch (error.status) {
        case 401:
          authService.logout();
          router.navigate(['/auth/login'], { queryParams: { reason: 'session_expired' } });
          break;

        case 403:
          router.navigate(['/forbidden']);
          break;

        case 500:
          snackBar.open(
            error.error?.message ?? 'An unexpected server error occurred. Please try again.',
            'Dismiss',
            { duration: 6000, panelClass: ['snack-error'] }
          );
          break;

        case 0:
          snackBar.open(
            'Unable to connect to the server. Check your network connection.',
            'Dismiss',
            { duration: 6000, panelClass: ['snack-error'] }
          );
          break;
      }

      return throwError(() => error);
    })
  );
};
