import { inject } from '@angular/core';
import { CanActivateFn, Router } from '@angular/router';
import { AuthService } from './auth';

export const authGuard: CanActivateFn = () =>
  inject(AuthService).isAuthenticated || inject(Router).createUrlTree(['/']);

export const guestGuard: CanActivateFn = () =>
  !inject(AuthService).isAuthenticated || inject(Router).createUrlTree(['/home']);
