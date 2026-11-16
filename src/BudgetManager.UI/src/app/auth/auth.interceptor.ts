import { HttpErrorResponse, HttpInterceptorFn } from '@angular/common/http';
import { inject } from '@angular/core';
import { Router } from '@angular/router';
import { catchError, throwError } from 'rxjs';
import { LedgerService } from '../shared/ledger';
import { AuthService } from './auth';

export const authInterceptor: HttpInterceptorFn = (req, next) => {
  const auth = inject(AuthService);
  const ledgers = inject(LedgerService);
  const router = inject(Router);
  const token = auth.token;
  const request = token ? req.clone({ setHeaders: { Authorization: `Bearer ${token}` } }) : req;
  return next(request).pipe(
    catchError((error: unknown) => {
      /*
       * A rejected or expired token can never succeed on retry: sign out and go to the login page.
       * Login and register answer 401 for bad credentials, which the forms handle themselves.
       * Reading the token drops an expired one, so null means it expired while the request was in flight; a different, newer token is never cleared.
       */
      const current = auth.token;
      if (
        error instanceof HttpErrorResponse &&
        error.status === 401 &&
        (current === token || current === null) &&
        !req.url.includes('/api/auth/')
      ) {
        auth.clear();
        ledgers.clearCurrentLedger();
        router.navigateByUrl('/');
      }
      return throwError(() => error);
    }),
  );
};
