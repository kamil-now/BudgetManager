import { Injectable, inject } from '@angular/core';
import { Router } from '@angular/router';
import { LedgerService } from '../shared/ledger';
import { AuthService } from './auth';

@Injectable({ providedIn: 'root' })
export class SessionService {
  private readonly auth = inject(AuthService);
  private readonly ledgers = inject(LedgerService);
  private readonly router = inject(Router);

  signOut(): void {
    this.auth.clear();
    this.ledgers.clearCurrentLedger();
    this.router.navigateByUrl('/');
  }
}
